# DartsStats

.NET Aspire application for tracking darts statistics.

## Requirements

- **.NET 10.0 SDK (Preview)** or later
- Docker Desktop (for Aspire resources: SQL Server, Redis, Keycloak)
- Visual Studio 2022 (17.13+) or VS Code with C# Dev Kit

## Migration History

- **2025-11-23**: Migrated from .NET 9.0 to .NET 10.0 (Preview)
  - All projects upgraded to `net10.0` target framework
  - NuGet packages updated to .NET 10 compatible versions
  - Successfully tested with Aspire 13.0

## Getting Started

1. Ensure .NET 10.0 SDK is installed:
   ```bash
   dotnet --version
   ```

2. Restore dependencies:
   ```bash
   dotnet restore
   ```

3. Run the Aspire AppHost:
   ```bash
   dotnet run --project DartsStats.AppHost
   ```

4. Access the Aspire Dashboard at: http://localhost:18888

## Projects

- **DartsStats.ServiceDefaults** - Shared service configuration and telemetry
- **DartsStats.Api** - ASP.NET Core Web API with EF Core and JWT authentication
- **DartsStats.AppHost** - .NET Aspire orchestration host

## Resources

- SQL Server (database)
- Redis (caching)
- Keycloak (authentication)
- Frontend (JavaScript/Vite)
