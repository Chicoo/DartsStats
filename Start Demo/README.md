# DartsStats Application

A full-stack darts statistics tracking application built with React (TypeScript) frontend and ASP.NET Core 9.0 backend, featuring JWT authentication via Keycloak, SQL Server database, and Redis caching.

## Table of Contents
- [Overview](#overview)
- [Architecture](#architecture)
- [Prerequisites](#prerequisites)
- [Project Structure](#project-structure)
- [Getting Started](#getting-started)
- [Configuration](#configuration)
- [Development](#development)
- [API Documentation](#api-documentation)
- [Authentication & Authorization](#authentication--authorization)
- [Database](#database)
- [Caching](#caching)
- [Troubleshooting](#troubleshooting)

## Overview

DartsStats is a comprehensive application for tracking and managing professional darts statistics including:
- Player profiles and statistics (average points, checkout percentages, etc.)
- Match records with detailed scoring
- Venue management
- Admin-only management features
- Secure authentication with Keycloak

## Architecture

### Frontend (`/client`)
- **Framework**: React 19.1.0 with TypeScript
- **Build Tool**: Vite 7.0.4
- **Routing**: React Router DOM 7.9.5
- **Authentication**: OIDC Client (oidc-client-ts)
- **Styling**: CSS modules

### Backend (`/server`)
- **Framework**: ASP.NET Core 9.0 (Web API)
- **Database**: SQL Server with Entity Framework Core 9.0
- **Caching**: Redis with StackExchange.Redis
- **Authentication**: JWT Bearer tokens via Keycloak
- **API Documentation**: Swagger/OpenAPI

## Prerequisites

Before starting development, ensure you have the following installed:

### Required
- **Node.js** 18.x or higher (for frontend)
- **.NET 9.0 SDK** (for backend)
- **SQL Server** (2019 or higher, or SQL Server Express)
- **Redis** (for caching)
- **Keycloak** (for authentication) or access to a Keycloak instance

### Recommended Tools
- **Visual Studio 2022** or **Visual Studio Code**
- **SQL Server Management Studio (SSMS)** or **Azure Data Studio**
- **Postman** or similar API testing tool
- **Git** for version control

## Project Structure

```
DartsStats/
├── client/                          # React frontend application
│   ├── src/
│   │   ├── components/             # React components
│   │   │   ├── Login.tsx          # Login page
│   │   │   ├── Management.tsx     # Admin management panel
│   │   │   ├── VenuePanel.tsx     # Venue listing
│   │   │   ├── AuthCallback.tsx   # OAuth callback handler
│   │   │   └── ProtectedRoute.tsx # Route protection HOC
│   │   ├── services/              # API and service layers
│   │   │   ├── api.ts            # Base API configuration
│   │   │   ├── authService.ts    # Authentication service
│   │   │   └── venueService.ts   # Venue API calls
│   │   ├── types/                # TypeScript type definitions
│   │   ├── App.tsx               # Main app component
│   │   └── main.tsx              # Application entry point
│   ├── public/                    # Static assets
│   ├── package.json              # Node dependencies
│   └── vite.config.ts            # Vite configuration
│
├── server/                         # ASP.NET Core backend
│   ├── Controllers/               # API endpoints
│   │   ├── AuthController.cs     # Authentication endpoints
│   │   ├── ManagementController.cs # Admin operations
│   │   ├── MatchesController.cs  # Match data endpoints
│   │   ├── PlayersController.cs  # Player data endpoints
│   │   └── VenuesController.cs   # Venue endpoints
│   ├── Data/
│   │   └── DartsDbContext.cs     # EF Core database context
│   ├── Models/                    # Data models and DTOs
│   │   ├── Player.cs
│   │   ├── Match.cs
│   │   ├── UpdateMatchDto.cs
│   │   └── CacheOptions.cs
│   ├── Services/                  # Business logic services
│   │   ├── DatabaseSeedService.cs # DB initialization
│   │   ├── DataSeedService.cs    # Seed data
│   │   ├── ICacheService.cs      # Cache interface
│   │   └── RedisCacheService.cs  # Redis implementation
│   ├── Properties/
│   │   └── launchSettings.json   # Launch configuration
│   ├── Program.cs                # Application startup
│   ├── appsettings.json          # Configuration template
│   └── DartsStats.Api.csproj     # Project file
│
└── DartsStats.sln                 # Visual Studio solution file
```

## Getting Started

### 1. Clone the Repository

```bash
git clone <repository-url>
cd Aspire_Demo
```

### 2. Set Up Infrastructure Services

#### SQL Server
1. Install SQL Server or use a cloud instance
2. Create a new database named `DartsStats`
3. Note your connection string

#### Redis
1. Install Redis locally or use a cloud service (Azure Redis Cache, AWS ElastiCache, etc.)
2. Start Redis server:
   ```bash
   # Windows (using WSL or Redis for Windows)
   redis-server
   
   # Or use Docker
   docker run -d -p 6379:6379 redis
   ```

#### Keycloak
1. Set up Keycloak server (local or cloud)
2. Create a new realm (e.g., `dartsstats`)
3. Create a client:
   - Client ID: `dartsstats-api`
   - Access Type: bearer-only (for API)
4. Create another client for the web app:
   - Client ID: `dartsstats-web`
   - Access Type: public
   - Valid Redirect URIs: `http://localhost:5173/*`
5. Create realm roles: `Admin`, `User`
6. Create test users and assign roles

### 3. Configure Backend

1. Navigate to the server directory:
   ```bash
   cd server
   ```

2. Create `appsettings.Development.json` (copy from `appsettings.json`):
   ```json
   {
     "Logging": {
       "LogLevel": {
         "Default": "Information",
         "Microsoft.AspNetCore": "Warning"
       }
     },
     "ConnectionStrings": {
       "dartsstats": "Server=localhost;Database=DartsStats;Integrated Security=true;TrustServerCertificate=true;",
       "Redis": "localhost:6379"
     },
     "Cache": {
       "VenueExpirationHours": 24,
       "DefaultExpirationHours": 1
     },
     "AllowedHosts": "*",
     "Keycloak": {
       "Authority": "http://localhost:8080/realms/dartsstats",
       "Audience": "dartsstats-api",
       "ClientId": "dartsstats-web"
     }
   }
   ```

3. Update the connection strings and Keycloak settings with your actual values

4. Install dependencies and run migrations:
   ```bash
   dotnet restore
   dotnet ef database update
   ```

5. Run the backend:
   ```bash
   dotnet run
   ```
   
   The API will be available at `http://localhost:5167` (or check `launchSettings.json`)

### 4. Configure Frontend

1. Navigate to the client directory:
   ```bash
   cd ../client
   ```

2. Create `.env.local` file in the client directory:
   ```env
   VITE_API_BASE_URL=http://localhost:5167
   VITE_KEYCLOAK_URL=http://localhost:8080
   VITE_KEYCLOAK_REALM=dartsstats
   VITE_KEYCLOAK_CLIENT_ID=dartsstats-web
   ```

3. Install dependencies:
   ```bash
   npm install
   ```

4. Run the development server:
   ```bash
   npm run dev
   ```
   
   The frontend will be available at `http://localhost:5173`

## Configuration

### Backend Configuration (`appsettings.json`)

| Setting | Description |
|---------|-------------|
| `ConnectionStrings:dartsstats` | SQL Server connection string |
| `ConnectionStrings:Redis` | Redis connection string |
| `Cache:VenueExpirationHours` | Cache expiration for venue data (hours) |
| `Cache:DefaultExpirationHours` | Default cache expiration (hours) |
| `Keycloak:Authority` | Keycloak realm URL |
| `Keycloak:Audience` | API audience identifier |
| `Keycloak:ClientId` | Web client ID |

### Frontend Configuration (`.env.local`)

| Variable | Description |
|----------|-------------|
| `VITE_API_BASE_URL` | Backend API base URL |
| `VITE_KEYCLOAK_URL` | Keycloak server URL |
| `VITE_KEYCLOAK_REALM` | Keycloak realm name |
| `VITE_KEYCLOAK_CLIENT_ID` | Keycloak client ID for frontend |

## Development

### Running in Development Mode

**Backend:**
```bash
cd server
dotnet watch run
```
Hot reload is enabled - changes will automatically restart the server.

**Frontend:**
```bash
cd client
npm run dev
```
Vite provides instant HMR (Hot Module Replacement).

### Building for Production

**Backend:**
```bash
cd server
dotnet publish -c Release -o ./publish
```

**Frontend:**
```bash
cd client
npm run build
```
Production build outputs to `client/dist/`

### Code Quality

**Frontend Linting:**
```bash
cd client
npm run lint
```

**Backend Code Analysis:**
```bash
cd server
dotnet format
```

## API Documentation

### Accessing Swagger UI

When the backend is running in development mode, navigate to:
```
http://localhost:5167/swagger
```

### Main Endpoints

#### Public Endpoints
- `GET /api/venues` - Get all venues
- `GET /api/players` - Get all players
- `GET /api/players/{id}` - Get player by ID
- `GET /api/matches` - Get all matches
- `GET /api/matches/{id}` - Get match by ID

#### Authentication Endpoints
- `POST /api/auth/login` - Login (redirects to Keycloak)
- `GET /api/auth/callback` - OAuth callback handler
- `POST /api/auth/refresh` - Refresh access token
- `POST /api/auth/logout` - Logout

#### Admin Endpoints (Requires Admin Role)
- `POST /api/management/seed-data` - Seed database with initial data
- `DELETE /api/management/clear-cache` - Clear Redis cache
- `PUT /api/matches/{id}` - Update match details
- `DELETE /api/matches/{id}` - Delete match

## Authentication & Authorization

### Flow
1. User clicks "Login" on the frontend
2. Redirected to Keycloak login page
3. After successful authentication, redirected back with authorization code
4. Frontend exchanges code for JWT tokens
5. JWT access token sent in `Authorization: Bearer <token>` header for API requests
6. Backend validates token with Keycloak

### Roles
- **User**: Can view all data
- **Admin**: Can view and modify all data, access management endpoints

### Token Storage
- Access tokens stored in `localStorage`
- Automatic token refresh before expiration
- Tokens cleared on logout

## Database

### Entity Framework Core Migrations

**Create a new migration:**
```bash
cd server
dotnet ef migrations add MigrationName
```

**Apply migrations:**
```bash
dotnet ef database update
```

**Rollback migration:**
```bash
dotnet ef database update PreviousMigrationName
```

### Database Schema

#### Players Table
- `Id` (int, PK)
- `Name` (string, 100 chars)
- `Country` (string, 50 chars)
- `AvgPoints` (decimal)
- `AvgLegDarts` (decimal)
- `CheckoutPercentage` (decimal)

#### Matches Table
- `Id` (int, PK)
- `Player1Id` (int, FK)
- `Player2Id` (int, FK)
- `MatchDate` (DateTime)
- `Season` (string, 10 chars)
- `Round` (string, 50 chars)
- `Player1Score` (int)
- `Player2Score` (int)

### Seeding Data

The application includes a data seeding service for development:

```bash
# Via API (requires Admin role)
POST /api/management/seed-data

# Or run on startup by uncommenting in Program.cs
```

## Caching

### Redis Implementation

The application uses Redis for distributed caching to improve performance:

- **Venue data**: Cached for 24 hours
- **Player/Match data**: Cached for 1 hour (configurable)
- **Cache invalidation**: Automatic on data updates

### Cache Service Usage

```csharp
// Get from cache or execute function
var players = await _cacheService.GetOrSetAsync(
    "players:all",
    async () => await _context.Players.ToListAsync(),
    TimeSpan.FromHours(1)
);
```

### Clear Cache

Admin users can clear the entire cache:
```bash
DELETE /api/management/clear-cache
```

## Troubleshooting

### Common Issues

#### Backend won't start
- **Error**: "Unable to connect to SQL Server"
  - Verify SQL Server is running
  - Check connection string in `appsettings.Development.json`
  - Ensure database exists

- **Error**: "Unable to connect to Redis"
  - Verify Redis is running (`redis-cli ping` should return "PONG")
  - Check Redis connection string

#### Frontend won't start
- **Error**: Module not found
  ```bash
  cd client
  rm -rf node_modules package-lock.json
  npm install
  ```

#### Authentication Issues
- **Error**: "401 Unauthorized"
  - Verify Keycloak is running and accessible
  - Check Keycloak configuration in both frontend and backend
  - Ensure user has correct roles assigned

- **Error**: "Invalid token"
  - Token may be expired - logout and login again
  - Check clock synchronization between services
  - Verify `Keycloak:Authority` matches realm URL exactly

#### CORS Errors
- Ensure backend CORS policy includes frontend URL
- Check `Program.cs` for CORS configuration
- Verify frontend is using correct API base URL

#### Database Migration Issues
- **Error**: "Build failed"
  ```bash
  cd server
  dotnet clean
  dotnet build
  dotnet ef migrations add InitialCreate --force
  ```

### Debug Mode

**Backend:**
- Set breakpoints in Visual Studio/VS Code
- Press F5 or use `dotnet run --launch-profile https`
- Check logs in console output

**Frontend:**
- Use browser DevTools (F12)
- Check Console for errors
- Use React DevTools extension
- Network tab for API call inspection

### Logging

Backend logs are output to console by default. Configure in `appsettings.json`:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Information"
    }
  }
}
```

## Additional Resources

- [ASP.NET Core Documentation](https://docs.microsoft.com/aspnet/core)
- [React Documentation](https://react.dev)
- [Entity Framework Core](https://docs.microsoft.com/ef/core)
- [Vite Documentation](https://vitejs.dev)
- [Keycloak Documentation](https://www.keycloak.org/documentation)
- [Redis Documentation](https://redis.io/documentation)

## Support

For questions or issues:
1. Check the [Troubleshooting](#troubleshooting) section
2. Review API documentation in Swagger UI
3. Check application logs
4. Contact the development team
