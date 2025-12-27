# DartsStats - Premier League Darts Statistics Application

A comprehensive full-stack application for tracking Premier League Darts statistics, featuring player standings, match results, and detailed analytics. Built with modern cloud-native architecture using .NET Aspire orchestration, containerized services, and enterprise-grade security.

## Table of Contents
- [Overview](#overview)
- [Features](#features)
- [Architecture](#architecture)
- [Demo Versions](#demo-versions)
- [Prerequisites](#prerequisites)
- [Getting Started](#getting-started)
- [Project Structure](#project-structure)
- [Development](#development)
- [Technologies Used](#technologies-used)
- [Aspire Orchestration](#aspire-orchestration)
- [Authentication & Security](#authentication--security)
- [API Documentation](#api-documentation)
- [Troubleshooting](#troubleshooting)

## Overview

DartsStats is a production-ready application showcasing modern .NET development practices with cloud-native architecture. It demonstrates the use of .NET Aspire for orchestrating distributed applications, including containerized databases, authentication services, caching, and API proxying.

## Features

### User Features
- **Player Statistics**: View comprehensive player standings with calculated averages, 180s, and highest checkouts
- **Match Results**: Browse match results by season (2024, 2025) with detailed scoring
- **Venue Management**: Track and manage venue information
- **Real Data**: Includes actual 2025 Premier League Darts statistics including finals results
- **Responsive UI**: Modern React-based interface with TypeScript

### Technical Features
- **Persistent Storage**: SQL Server database with Entity Framework Core migrations
- **Caching Layer**: Redis for high-performance data caching
- **Authentication**: Keycloak-based JWT authentication with role-based access control
- **API Security**: Protected endpoints with JWT bearer tokens
- **API Monitoring**: Dev Proxy integration for API testing and mocking
- **Observability**: Built-in telemetry and structured logging via Aspire
- **Container Orchestration**: Fully containerized services managed by .NET Aspire
- **Database Tools**: DbGate integration for database management
- **Redis Management**: Redis Commander and RedisInsight for cache inspection

## Architecture

### Technology Stack

**Frontend** (`/client`)
- React 19.1.0 with TypeScript 5.8
- Vite 7.0.4 for blazing-fast development
- React Router DOM 7.9.5 for routing
- OIDC Client (oidc-client-ts 3.3.0) for authentication
- CSS Modules for styling

**Backend** (`/server`)
- .NET 10.0 with ASP.NET Core Web API
- Entity Framework Core 10.0 with SQL Server provider
- JWT Bearer authentication via Keycloak
- Redis caching with StackExchange.Redis
- Scalar for interactive API documentation (OpenAPI 3.0)

**Infrastructure & Orchestration**
- **.NET Aspire 13.1.0**: Application orchestration and service management
- **SQL Server**: Persistent database with data volumes
- **Redis 6.x**: Distributed caching layer
- **Keycloak**: Enterprise identity and access management
- **Dev Proxy**: API request interception and testing
- **Docker Compose**: Container deployment and networking
- **DbGate**: Web-based database administration
- **Redis Commander & RedisInsight**: Redis management tools

## Demo Versions

This repository contains two versions of the application to support different learning scenarios:

### Start Demo (`/Start Demo`)
- Basic implementation without Aspire orchestration
- Manual configuration of all services required
- Ideal for understanding individual components
- Requires manual setup of SQL Server, Redis, and Keycloak
- Demonstrates traditional multi-tier application architecture

### Full Demo (`/Full Demo`) - **Recommended**
- Complete implementation with .NET Aspire orchestration
- Automatic service provisioning and configuration
- Includes all enterprise features (Keycloak, Redis, Dev Proxy)
- Container lifecycle management with persistent volumes
- Production-ready architecture with observability

**For most users, start with the Full Demo** as it provides the best experience with minimal setup.

## Prerequisites

### Required
- **.NET 10.0 SDK** or later
- **Node.js 18.x** or higher (includes npm)
- **Docker Desktop** (with WSL 2 backend on Windows)
  - Ensure Docker is running before starting the application
  - Required for SQL Server, Redis, and Keycloak containers

### Recommended Tools
- **Visual Studio 2022** or **Visual Studio Code** with C# extensions
- **Azure Data Studio** or **SQL Server Management Studio** for database management
- **Git** for version control
- **Postman** or similar API testing tool (optional, Scalar provides interactive docs)

### Important Notes
- The Aspire workload is obsolete and should not be installed
- All dependencies are managed via NuGet packages in the project files
- Docker Desktop must have sufficient resources allocated (4GB+ RAM recommended)

## Getting Started

### Quick Start (Full Demo - Recommended)

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd DartsStats
   ```

2. **Ensure Docker Desktop is running**
   - Start Docker Desktop and wait for it to be fully initialized
   - Verify with: `docker ps`

3. **Install frontend dependencies**
   ```bash
   cd "Full Demo/client"
   npm install
   cd ../..
   ```

4. **Run the application with Aspire**
   ```bash
   cd "Full Demo"
   aspire run
   ```
   
   Or using dotnet:
   ```bash
   dotnet run --project "Full Demo/DartsStats.AppHost/DartsStats.AppHost.csproj"
   ```

5. **Access the Aspire Dashboard**
   - The dashboard opens automatically in your browser
   - Default URL: http://localhost:15888 (or custom port shown in terminal)
   - Monitor all resources, view logs, and inspect telemetry

6. **Access the application**
   - Frontend: Check the Aspire dashboard for the frontend URL
   - API: Check the Aspire dashboard for the API URL
   - API Documentation (Scalar): Available at `{API_URL}/scalar/v1`
   - Keycloak Admin: http://localhost:8080 (admin/admin)

### First-Time Setup Notes

- **Database**: SQL Server container will auto-create and seed the database
- **Keycloak**: Realm and users are auto-imported from `/data/keycloak`
- **Redis**: Starts empty; data is cached on first API requests
- **Dev Proxy**: Configured but set to explicit start (start manually from dashboard)

### Manual Setup (Start Demo)

If you prefer to run services manually without Aspire, see the detailed README in the `/Start Demo` folder.

## Project Structure

```
DartsStats/
├── Full Demo/                          # Complete Aspire-orchestrated application
│   ├── DartsStats.sln                 # Main solution file
│   ├── DartsStats.AppHost/            # .NET Aspire orchestration
│   │   ├── AppHost.cs                # Service configuration and composition
│   │   ├── AppHostExtensions.cs      # Custom Aspire extensions
│   │   └── DartsStats.AppHost.csproj # Aspire host project
│   ├── DartsStats.ServiceDefaults/    # Shared service configuration
│   │   └── Extensions.cs             # Common service extensions
│   ├── server/                        # ASP.NET Core Web API
│   │   ├── Controllers/              # API endpoints
│   │   │   ├── AuthController.cs    # Authentication
│   │   │   ├── ManagementController.cs # Admin operations
│   │   │   ├── MatchesController.cs # Match data
│   │   │   ├── PlayersController.cs # Player statistics
│   │   │   └── VenuesController.cs  # Venue management
│   │   ├── Data/                    # Database context and migrations
│   │   │   └── DartsDbContext.cs   # EF Core DbContext
│   │   ├── Models/                  # Domain models and DTOs
│   │   ├── Entities/               # Database entities
│   │   ├── DTOs/                   # Data transfer objects
│   │   ├── Services/               # Business logic
│   │   │   ├── DatabaseSeedService.cs # Database initialization
│   │   │   ├── DataSeedService.cs    # Seed data
│   │   │   ├── ICacheService.cs      # Cache interface
│   │   │   └── RedisCacheService.cs  # Redis implementation
│   │   ├── Mappings/               # Object mappings
│   │   └── Program.cs              # API startup configuration
│   ├── client/                      # React TypeScript frontend
│   │   ├── src/
│   │   │   ├── components/         # React components
│   │   │   │   ├── Login.tsx      # Authentication UI
│   │   │   │   ├── Management.tsx # Admin panel
│   │   │   │   ├── VenuePanel.tsx # Venue management
│   │   │   │   ├── AuthCallback.tsx # OAuth callback
│   │   │   │   └── ProtectedRoute.tsx # Route guards
│   │   │   ├── services/          # API and service layers
│   │   │   │   ├── api.ts        # Axios configuration
│   │   │   │   ├── authService.ts # Authentication logic
│   │   │   │   └── venueService.ts # Venue API
│   │   │   ├── types/            # TypeScript definitions
│   │   │   ├── App.tsx           # Main application
│   │   │   └── main.tsx          # Entry point
│   │   ├── public/               # Static assets
│   │   ├── package.json          # Node dependencies
│   │   └── vite.config.ts        # Vite configuration
│   └── data/                      # Configuration data
│       ├── keycloak/             # Keycloak realm import
│       └── devproxy/             # Dev Proxy configuration
│
├── Start Demo/                    # Basic implementation without Aspire
│   ├── DartsStats.sln
│   ├── server/                   # Similar structure to Full Demo
│   └── client/                   # Similar structure to Full Demo
│
├── README.md                     # This file
└── AGENTS.md                     # Copilot agent instructions
```

## Development

### Using Visual Studio Code

The workspace includes pre-configured tasks for common operations:

1. Press `Ctrl+Shift+P` (or `Cmd+Shift+P` on macOS)
2. Type "Tasks: Run Task"
3. Select:
   - **`run-aspire`**: Start entire application with Aspire (recommended)
   - **`run-api`**: Start only the backend API
   - **`run-client`**: Start only the frontend dev server
   - **`build`**: Build the API project

### Aspire CLI Commands

```bash
# Run the application
aspire run

# Update Aspire to latest version
aspire update

# Generate deployment manifests
aspire publish
```

### Working with Resources

Use the **Aspire MCP tools** available in Copilot:
- **List resources**: Check status of all services
- **Execute resource command**: Start, stop, or restart services
- **List structured logs**: View application logs
- **List console logs**: View container output
- **List traces**: Inspect distributed traces

### Database Management

- **Migrations**: EF Core migrations are automatically applied on startup
- **DbGate**: Access via Aspire dashboard to browse and edit database
- **Seeding**: Database is automatically seeded with Premier League Darts data

### Cache Management

- **Redis Commander**: Web UI for Redis management (available via dashboard)
- **RedisInsight**: Advanced Redis GUI (available via dashboard)
- **Clear cache**: Use the "Clear Redis Data" command in the Aspire dashboard

## Technologies Used

### Orchestration & Infrastructure
- **.NET Aspire 13.1.0**: Cloud-native orchestration
- **Docker Compose**: Container networking and deployment
- **Aspire.Hosting.SqlServer**: SQL Server container management
- **Aspire.Hosting.Redis**: Redis container management
- **Aspire.Hosting.Keycloak**: Identity provider integration
- **Aspire.Hosting.JavaScript**: Vite app hosting
- **DevProxy.Hosting**: API proxy and testing

### Backend
- **.NET 10.0**: Latest .NET runtime
- **ASP.NET Core Web API**: RESTful API framework
- **Entity Framework Core 10.0**: ORM with SQL Server provider
- **SQL Server**: Relational database (containerized)
- **StackExchange.Redis**: Redis client library
- **Microsoft.AspNetCore.Authentication.JwtBearer**: JWT authentication
- **Scalar.AspNetCore**: Interactive API documentation (OpenAPI)

### Frontend
- **React 19.1.0**: UI framework
- **TypeScript 5.8**: Type-safe JavaScript
- **Vite 7.0.4**: Build tool and dev server
- **React Router DOM 7.9.5**: Client-side routing
- **oidc-client-ts 3.3.0**: OpenID Connect authentication
- **Axios**: HTTP client (via api.ts)

### Additional Tools
- **DbGate**: Database administration UI
- **Redis Commander**: Redis management UI
- **RedisInsight**: Advanced Redis client
- **Dev Proxy 0.2.2**: API mocking and testing

## Aspire Orchestration

### Key Features

1. **Resource Management**
   - Automatic container lifecycle management
   - Persistent volumes for data retention
   - Health checks and automatic restarts
   - Service discovery and networking

2. **Observability**
   - Structured logging aggregation
   - Distributed tracing (OpenTelemetry)
   - Metrics collection and visualization
   - Real-time resource monitoring

3. **Development Experience**
   - Single command to start all services
   - Automatic dependency ordering (WaitFor)
   - Environment variable injection
   - Hot reload for frontend and backend

4. **Production Ready**
   - Docker Compose generation for deployment
   - Container image building
   - Kubernetes manifest generation
   - Resource scaling configuration

### Resource Configuration

The AppHost configures:
- **SQL Server**: Persistent database with DbGate UI
- **Redis**: Distributed cache with management tools
- **Keycloak**: Identity provider with realm import
- **Dev Proxy**: API testing with certificate trust
- **API**: Backend service with dependencies
- **Frontend**: Vite development server with API reference

## Authentication & Security

### Keycloak Configuration

**Default Credentials**
- Admin Console: http://localhost:8080
- Username: `admin`
- Password: `admin`

**Realm**: `dartsstats`
- Automatically imported from `/data/keycloak` on first start
- Pre-configured with clients, roles, and test users

**Roles**
- `Admin`: Full access to management endpoints
- `User`: Read-only access to statistics

**Clients**
- `dartsstats-api`: Bearer-only client for API
- `dartsstats-web`: Public client for SPA

### JWT Authentication

The API uses JWT bearer tokens issued by Keycloak:
- Tokens are validated against Keycloak's public keys
- Role claims are mapped to ASP.NET Core authorization policies
- All `/api/management/*` endpoints require `Admin` role

## API Documentation

### Interactive Documentation (Scalar)

Access Scalar at `{API_URL}/scalar/v1` for:
- Interactive API exploration
- Request/response examples
- Authentication testing
- Schema documentation

### Key Endpoints

**Players**
- `GET /api/players` - List all players with statistics
- `GET /api/players/{id}` - Get player details
- `GET /api/players/search?name={name}` - Search players

**Matches**
- `GET /api/matches` - List all matches
- `GET /api/matches/{id}` - Get match details
- `GET /api/matches/season/{year}` - Filter by season

**Venues**
- `GET /api/venues` - List all venues
- `POST /api/venues` - Create venue (Admin only)
- `PUT /api/venues/{id}` - Update venue (Admin only)

**Management** (Admin only)
- `POST /api/management/matches` - Create match
- `PUT /api/management/matches/{id}` - Update match
- `DELETE /api/management/matches/{id}` - Delete match

## Troubleshooting

### Common Issues

**Docker Desktop not running**
```
Error: Cannot connect to Docker daemon
Solution: Start Docker Desktop and wait for initialization
```

**Port conflicts**
```
Error: Port 8080 already in use
Solution: Stop other services using the port or modify port in AppHost.cs
```

**Keycloak not starting**
```
Solution: Check Docker container logs in Aspire dashboard
Verify realm import files exist in Full Demo/data/keycloak
```

**Database connection issues**
```
Solution: Ensure SQL Server container is healthy (check Aspire dashboard)
Wait for database to fully initialize (first start takes longer)
```

**Frontend cannot reach API**
```
Solution: Verify API is running and accessible
Check VITE_API_BASE_URL environment variable in Aspire dashboard
Ensure no CORS issues (check browser console)
```

### Resetting State

**Clear all data and restart fresh:**

1. Stop the Aspire application
2. Remove Docker volumes:
   ```bash
   docker volume ls | grep dartsstats
   docker volume rm <volume-names>
   ```
3. Restart with `aspire run`

**Clear only Redis cache:**
- Use "Clear Redis Data" command in Aspire dashboard

### Getting Help

- Check Aspire dashboard logs for detailed error messages
- Review structured logs for specific resources
- Inspect traces for request flow debugging
- Consult official documentation:
  - https://aspire.dev
  - https://learn.microsoft.com/dotnet/aspire
  - https://www.keycloak.org/docs/

### Aspire Best Practices

1. Always start from a known state by checking resource status in the dashboard
2. Make incremental changes and validate via Aspire dashboard
3. Restart the AppHost after modifying `AppHost.cs`
4. Use Aspire MCP tools for debugging before modifying code
5. Avoid persistent containers early in development to prevent state issues
