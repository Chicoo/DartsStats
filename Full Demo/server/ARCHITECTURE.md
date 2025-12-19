# DTO and Entity Separation

This document explains the separation between Data Transfer Objects (DTOs) and Entity Framework models in the DartsStats API.

## Architecture Overview

The application now follows a clean architecture pattern with clear separation of concerns:

```
┌─────────────────┐
│   Controllers   │  ← API Layer (uses DTOs)
└────────┬────────┘
         │
┌────────▼────────┐
│    Mappings     │  ← Converts between DTOs and Entities
└────────┬────────┘
         │
┌────────▼────────┐
│   DbContext     │  ← Data Layer (uses Entities)
└────────┬────────┘
         │
┌────────▼────────┐
│    Database     │
└─────────────────┘
```

## Directory Structure

```
server/
├── Controllers/         # API controllers (use DTOs)
├── DTOs/               # Data Transfer Objects
│   ├── PlayerDto.cs
│   ├── MatchDto.cs
│   └── UpdateMatchDto.cs
├── Entities/           # EF Core entity models
│   ├── PlayerEntity.cs
│   └── MatchEntity.cs
├── Mappings/           # Conversion logic
│   └── MappingExtensions.cs
├── Data/               # DbContext
│   └── DartsDbContext.cs
└── Services/           # Business logic
```

## Components

### 1. Entities (`/Entities`)

Entity models represent the database schema and are used exclusively by Entity Framework Core.

**Naming Convention:** `{EntityName}Entity.cs`

**Purpose:**
- Map to database tables
- Define relationships
- Contain EF-specific configurations
- Never exposed directly through API endpoints

**Example:**
```csharp
// PlayerEntity.cs
namespace DartsStats.Api.Entities;

public class PlayerEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    // ... database fields
}
```

### 2. DTOs (`/DTOs`)

Data Transfer Objects are implemented as **records** for API communication between client and server.

**Naming Convention:** `{Purpose}{EntityName}Dto.cs`

**Why Records?**
- Immutable by default (better for data transfer)
- Value-based equality (two DTOs with same values are equal)
- Concise syntax with positional parameters
- Built-in `with` expressions for creating modified copies
- Thread-safe
- Perfect for representing data contracts

**Purpose:**
- Define API contracts
- Control what data is exposed
- Validate input data
- Version API independently from database

**Types of DTOs:**
- **Read DTOs:** `PlayerDto`, `MatchDto` - For GET requests
- **Update DTOs:** `UpdateMatchDto` - For PUT requests (no ID in body, used for updates)

**Example:**
```csharp
// PlayerDto.cs - Read DTO
namespace DartsStats.Api.DTOs;

public record PlayerDto(
    int Id,
    string Name,
    string Country,
    int MatchesPlayed,
    int MatchesWon
    // ... API fields
);

// MatchDto.cs - Read DTO with nested objects
namespace DartsStats.Api.DTOs;

public record MatchDto(
    int Id,
    int Player1Id,
    int Player2Id,
    PlayerDto? Player1,
    PlayerDto? Player2,
    DateTime MatchDate,
    int Player1Score,
    int Player2Score
    // ... more fields
);

// UpdateMatchDto.cs - Update DTO (no ID)
namespace DartsStats.Api.DTOs;

public record UpdateMatchDto(
    int Player1Id,
    int Player2Id,
    DateTime MatchDate,
    int Player1Score,
    int Player2Score
    // ... no Id property
);
```

**Record Benefits:**
```csharp
// Value-based equality
var player1 = new PlayerDto(1, "Luke Humphries", "England", 39, 25);
var player2 = new PlayerDto(1, "Luke Humphries", "England", 39, 25);
player1 == player2; // true (same values)

// Non-destructive mutation with 'with' expressions
var updatedPlayer = player1 with { MatchesWon = 26 };
// player1 is unchanged, updatedPlayer has MatchesWon = 26

// Deconstruction
var (id, name, country, _, _) = player1;
Console.WriteLine($"{name} from {country}");
```

### 3. Mappings (`/Mappings`)

Extension methods for converting between Entities and DTOs (records).

**Purpose:**
- Centralize conversion logic
- Keep controllers and services clean
- Maintain consistency

**Example:**
```csharp
// MappingExtensions.cs
namespace DartsStats.Api.Mappings;

public static class MappingExtensions
{
    // Entity to DTO (using record positional constructor)
    public static PlayerDto ToDto(this PlayerEntity entity)
    {
        return new PlayerDto(
            entity.Id,
            entity.Name,
            entity.Country,
            entity.MatchesPlayed,
            entity.MatchesWon
            // ... all properties in order
        );
    }

    // Entity to DTO for matches (with nested DTOs)
    public static MatchDto ToDto(this MatchEntity entity)
    {
        return new MatchDto(
            entity.Id,
            entity.Player1Id,
            entity.Player2Id,
            entity.Player1?.ToDto(),  // Nested DTO
            entity.Player2?.ToDto(),  // Nested DTO
            entity.MatchDate,
            entity.Player1Score,
            entity.Player2Score
            // ...
        );
    }

    // Update Entity from DTO (immutable records, but entity is mutable)
    public static void UpdateEntity(this UpdateMatchDto dto, MatchEntity entity)
    {
        entity.Player1Id = dto.Player1Id;
        entity.Player2Id = dto.Player2Id;
        entity.MatchDate = dto.MatchDate;
        entity.Player1Score = dto.Player1Score;
        entity.Player2Score = dto.Player2Score;
        // ...
    }
}
```

### 4. DbContext (`/Data`)

Database context now uses Entity models with explicit table naming.

**Example:**
```csharp
// DartsDbContext.cs
public class DartsDbContext : DbContext
{
    public DbSet<PlayerEntity> Players { get; set; }
    public DbSet<MatchEntity> Matches { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PlayerEntity>(entity =>
        {
            entity.ToTable("Players");
            entity.HasKey(e => e.Id);
            // ... configuration
        });

        modelBuilder.Entity<MatchEntity>(entity =>
        {
            entity.ToTable("Matches");
            // ... configuration
        });
    }
}
```

### 5. Controllers

Controllers use DTOs for all API communication.

**Example:**
```csharp
[ApiController]
[Route("api/[controller]")]
public class PlayersController : ControllerBase
{
    private readonly DartsDbContext _context;

    // Returns DTOs
    [HttpGet]
    public async Task<ActionResult<IEnumerable<PlayerDto>>> GetPlayers()
    {
        var players = await _context.Players
            .OrderBy(p => p.Position)
            .ToListAsync();
        
        return Ok(players.Select(p => p.ToDto()));
    }

    // Returns a single DTO
    [HttpGet("{id}")]
    public async Task<ActionResult<PlayerDto>> GetPlayer(int id)
    {
        var player = await _context.Players.FindAsync(id);
        if (player == null)
        {
            return NotFound();
        }
        return Ok(player.ToDto());
    }
}

[ApiController]
[Route("api/[controller]")]
public class ManagementController : ControllerBase
{
    private readonly DartsDbContext _context;

    // Accepts an Update DTO
    [HttpPut("matches/{id}")]
    public async Task<ActionResult<MatchDto>> UpdateMatch(int id, UpdateMatchDto dto)
    {
        var entity = await _context.Matches.FindAsync(id);
        if (entity == null) return NotFound();
        
        // Update entity using mapping extension
        dto.UpdateEntity(entity);
        
        await _context.SaveChangesAsync();
        
        // Reload navigation properties
        await _context.Entry(entity).Reference(m => m.Player1).LoadAsync();
        await _context.Entry(entity).Reference(m => m.Player2).LoadAsync();
        
        return Ok(entity.ToDto());
    }

    // Get match for editing
    [HttpGet("matches/{id}")]
    public async Task<ActionResult<MatchDto>> GetMatchForEdit(int id)
    {
        var match = await _context.Matches
            .Include(m => m.Player1)
            .Include(m => m.Player2)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (match == null)
        {
            return NotFound();
        }

        return Ok(match.ToDto());
    }
}
```

## Benefits

### 1. **Separation of Concerns**
- Database schema can change without affecting API contracts
- API can evolve independently from database structure

### 2. **Security**
- Control exactly what data is exposed through the API
- Prevent over-posting attacks
- Hide internal implementation details

### 3. **Flexibility**
- Different DTOs for different operations (Create vs Update vs Read)
- Flatten complex relationships for API responses
- Add computed properties without affecting database

### 4. **Versioning**
- Support multiple API versions with different DTOs
- Maintain backward compatibility

### 5. **Validation**
- Apply different validation rules for different operations
- Validation attributes on DTOs don't clutter entities

### 6. **Immutability** (with Records)
- DTOs are immutable, preventing accidental modification
- Thread-safe data transfer
- Value-based equality for easier testing and comparison

## Best Practices

### 1. Never Expose Entities Directly
```csharp
// ❌ BAD
[HttpGet]
public async Task<ActionResult<IEnumerable<PlayerEntity>>> GetPlayers()
{
    return await _context.Players.ToListAsync();
}

// ✅ GOOD
[HttpGet]
public async Task<ActionResult<IEnumerable<PlayerDto>>> GetPlayers()
{
    var players = await _context.Players.ToListAsync();
    return Ok(players.Select(p => p.ToDto()));
}
```

### 2. Use Records for DTOs
```csharp
// ✅ Records provide immutability and value equality
public record PlayerDto(
    int Id,
    string Name,
    string Country
);

// ❌ Avoid mutable classes for DTOs
public class PlayerDto
{
    public int Id { get; set; }
    public string Name { get; set; }
}
```

### 3. Use Appropriate DTOs for Operations
```csharp
// ✅ Update operations - no ID in body (ID in route)
public record UpdateMatchDto(
    int Player1Id,
    int Player2Id,
    DateTime MatchDate,
    int Player1Score,
    int Player2Score
);

// ✅ Read operations - include ID and navigation properties
public record MatchDto(
    int Id,
    int Player1Id,
    int Player2Id,
    PlayerDto? Player1,
    PlayerDto? Player2,
    DateTime MatchDate,
    int Player1Score,
    int Player2Score
);

// ✅ Simple read DTOs
public record PlayerDto(
    int Id,
    string Name,
    string Country
);
```

### 4. Keep Mappings Simple with Records
```csharp
// ✅ Simple, clear mappings using record positional constructor
public static PlayerDto ToDto(this PlayerEntity entity)
{
    return new PlayerDto(
        entity.Id,
        entity.Name,
        entity.Country
    );
}

// Alternative: Use object initializer if you need to skip some properties
public static PlayerDto ToDto(this PlayerEntity entity)
{
    return new PlayerDto
    {
        Id = entity.Id,
        Name = entity.Name,
        Country = entity.Country
    };
}
```

### 5. Configure EF Relationships on Entities
```csharp
// Entity configuration stays in DbContext
modelBuilder.Entity<MatchEntity>(entity =>
{
    entity.HasOne(e => e.Player1)
          .WithMany()
          .HasForeignKey(e => e.Player1Id)
          .OnDelete(DeleteBehavior.Restrict);
});
```

### 6. Leverage Record Features
```csharp
// ✅ Use 'with' expressions for creating modified copies
var updatedDto = originalDto with { Name = "New Name" };

// ✅ Use deconstruction for extracting values
var (id, name, country) = playerDto;

// ✅ Value-based equality in tests
Assert.Equal(expected, actual); // compares values, not references
```

## Migration Guide

### Current DTO Structure

The application currently uses the following DTOs:
- **PlayerDto** - Read-only DTO for player data (includes all fields)
- **MatchDto** - Read-only DTO for match data (includes nested PlayerDto objects)
- **UpdateMatchDto** - Used for updating matches (no ID field, as ID comes from route)

**Note:** There is no separate `CreateMatchDto`. If you need to add creation functionality, you can either:
1. Reuse `UpdateMatchDto` for creation (as it contains all necessary fields except ID)
2. Create a dedicated `CreateMatchDto` if creation requires different fields than updates

### Adding a New Field

1. **Add to Entity** (`/Entities`)
   ```csharp
   public class PlayerEntity
   {
       // ...existing properties
       public string Email { get; set; } = string.Empty;
   }
   ```

2. **Add to DTO Record** (`/DTOs`) - if it should be exposed via API
   ```csharp
   // Add the new parameter to the record
   public record PlayerDto(
       int Id,
       string Name,
       string Country,
       string Email  // New field added at the end
   );
   ```
   
   **Note:** When adding parameters to records, add them at the end to avoid breaking changes if other code uses positional construction.

3. **Update Mapping** (`/Mappings`)
   ```csharp
   public static PlayerDto ToDto(this PlayerEntity entity)
   {
       return new PlayerDto(
           entity.Id,
           entity.Name,
           entity.Country,
           entity.Email  // Include new field in order
       );
   }
   ```

4. **Create EF Migration**
   ```bash
   dotnet ef migrations add AddEmailToPlayer
   dotnet ef database update
   ```

### Adding a New DTO

If you need to add a new DTO (e.g., `CreateMatchDto` for creation operations):

1. **Create the DTO** (`/DTOs`)
   ```csharp
   namespace DartsStats.Api.DTOs;

   public record CreateMatchDto(
       int Player1Id,
       int Player2Id,
       DateTime MatchDate,
       int Player1Score,
       int Player2Score
       // ... fields needed for creation (no ID)
   );
   ```

2. **Add Mapping** (`/Mappings`)
   ```csharp
   public static MatchEntity ToEntity(this CreateMatchDto dto)
   {
       return new MatchEntity
       {
           Player1Id = dto.Player1Id,
           Player2Id = dto.Player2Id,
           MatchDate = dto.MatchDate,
           Player1Score = dto.Player1Score,
           Player2Score = dto.Player2Score
           // ... map all fields except ID
       };
   }
   ```

3. **Use in Controller**
   ```csharp
   [HttpPost]
   public async Task<ActionResult<MatchDto>> CreateMatch(CreateMatchDto dto)
   {
       var entity = dto.ToEntity();
       _context.Matches.Add(entity);
       await _context.SaveChangesAsync();
       
       // Load navigation properties
       await _context.Entry(entity).Reference(m => m.Player1).LoadAsync();
       await _context.Entry(entity).Reference(m => m.Player2).LoadAsync();
       
       return CreatedAtAction(
           nameof(GetMatch), 
           new { id = entity.Id }, 
           entity.ToDto()
       );
   }
   ```
