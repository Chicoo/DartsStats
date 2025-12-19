using Microsoft.EntityFrameworkCore;
using DartsStats.Api.Data;
using DartsStats.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi(); 

// Add Entity Framework with connection string from Aspire
builder.Services.AddDbContext<DartsDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("dartsstats"));
});

// Add our database seeding service
builder.Services.AddScoped<DatabaseSeedService>();

// Add Redis distributed cache
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "DartsStats";
});

// Add cache service
builder.Services.AddScoped<ICacheService, RedisCacheService>();


// Add HttpClient for external API calls
builder.Services.AddHttpClient();

// Add JWT Bearer Authentication for Keycloak
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var keycloakAuthority = builder.Configuration["Keycloak:Authority"];
        var keycloakAudience = builder.Configuration["Keycloak:Audience"];

        options.Authority = keycloakAuthority;
        options.Audience = keycloakAudience;
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidAudience = keycloakAudience,
            ValidIssuer = keycloakAuthority,
            ClockSkew = TimeSpan.Zero
        };
    });

// Add Authorization with Admin policy
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireRole("admin");
    });
});

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
// Use CORS first, before other middleware
app.UseCors("AllowFrontend");

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();

    // Redirect root to Scalar endpoint in development
    app.MapGet("/", () => Results.Redirect("/scalar/v1")).ExcludeFromDescription();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Seed data
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<DartsDbContext>();
    await context.Database.EnsureCreatedAsync();
    
    var seedService = scope.ServiceProvider.GetRequiredService<DatabaseSeedService>();
    await seedService.SeedDataAsync();
}

app.Run();
