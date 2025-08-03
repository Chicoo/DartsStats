# Premier League Darts Statistics

This application shows statistics for the Premier League Darts competition, including player standings, match results, and detailed statistics with persistent SQL Server database storage.

## Features

- View player standings with calculated statistics
- See match results by season (2024, 2025)
- Track player averages, 180s, and highest checkouts
- Filter matches by season
- **Real 2025 Premier League Darts data** including finals results
- **Persistent SQL Server database** with Entity Framework Core
- **Containerized development** with SQL Server running in Docker via Aspire

## Prerequisites

- .NET 9.0 SDK
- Node.js (v16 or higher)
- npm (comes with Node.js)
- **Docker Desktop** (for SQL Server container via Aspire)

## Getting Started

### Option 1: Using .NET Aspire (Recommended)
.NET Aspire orchestrates both the backend and frontend automatically.

1. Clone this repository
2. Install npm dependencies:
   ```
   cd client
   npm install
   cd ..
   ```
3. Run the entire application with Aspire:
   ```
   dotnet run --project DartsStats.AppHost/DartsStats.AppHost.csproj
   ```
4. Open the Aspire Dashboard that opens automatically to see all services

### Option 2: Manual Setup
1. Clone this repository
2. Start the backend API:
   ```
   cd server
   dotnet run
   ```
3. Start the frontend development server:
   ```
   cd client
   npm install
   npm run dev
   ```
4. Open your browser and navigate to http://localhost:5173

## Project Structure

- `/DartsStats.AppHost` - .NET Aspire orchestration project
- `/DartsStats.ServiceDefaults` - Shared Aspire service configuration
- `/server` - Backend API project
  - `/Controllers` - API controllers
  - `/Models` - Data models
  - `/Properties` - Launch settings and configuration
- `/client` - React frontend application
  - `/src` - Source code
  - `/types` - TypeScript interfaces
  - `/services` - API service functions
  - `/components` - React components (future)

## Development

You can use VS Code tasks to run the application:

1. Press `Ctrl+Shift+P` (or `Cmd+Shift+P` on macOS)
2. Type "Tasks: Run Task"
3. Choose:
   - `run-aspire` to start the entire application with Aspire (recommended)
   - `run-api` to start only the backend
   - `run-client` to start only the frontend

## Technologies Used

- **Orchestration:**
  - .NET Aspire 9.4 (with SQL Server container orchestration)
- **Backend:**
  - .NET 9
  - ASP.NET Core Web API
  - **Entity Framework Core** (with SQL Server provider)
  - **SQL Server** (containerized via Aspire)
- **Frontend:**
  - React with modern UI design
  - TypeScript
  - Vite
- **Data:**
  - **Real 2025 Premier League Darts statistics**
  - **Automated database seeding**
  - **Persistent SQL Server storage**
