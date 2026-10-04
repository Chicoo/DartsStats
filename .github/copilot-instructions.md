# Copilot Instructions

## Architecture Overview

**DartsStats** is a Premier League Darts statistics app with three main components:

- **`client/`** — React 19 + TypeScript SPA (Vite), handles player standings, match history, and an admin management section
- **`server/`** (`DartsStats.Api`) — ASP.NET Core Web API backed by SQL Server (EF Core) with Redis caching and Keycloak JWT auth
- **`DartsStats.AppHost/`** — .NET Aspire orchestrator that wires everything together

The AppHost (`Full Demo/DartsStats.AppHost/AppHost.cs`) provisions: SQL Server + DbGate, Keycloak (realm imported from `data/keycloak/`), Redis + RedisInsight/Commander, Dev Proxy, the API project, and the Vite frontend. All services run as Docker containers or .NET projects coordinated by Aspire.

The frontend gets the API URL via `VITE_API_BASE_URL` injected by Aspire. The API gets its Keycloak authority via `Keycloak__Authority` environment variable.

## Running the Application

```bash
aspire run
```

This starts all resources. Only restart if `AppHost.cs` changes. Use the Aspire MCP tools to check resource status and diagnose issues before modifying code.

## Build, Lint & Test

**Client** (from `Full Demo/client/`):
```bash
npm run dev          # start Vite dev server
npm run build        # tsc -b && vite build
npm run lint         # ESLint
```

**Server** (from `Full Demo/server/`):
```bash
dotnet build
dotnet run
```

**E2E tests** (from `Full Demo/tests/e2e/`):
```bash
npx playwright test
npx playwright test --project=chromium          # single browser
npx playwright test tests/sometest.spec.ts      # single test file
```

## Server Conventions

### Entity / DTO Separation
- **Entities** (`/Entities/`) — EF Core models only, never returned from controllers. Named `{Name}Entity.cs`.
- **DTOs** (`/DTOs/`) — implemented as C# **records** (immutable, value equality). Named `{Purpose}{Name}Dto.cs`.
  - Read DTOs include `Id` and nested navigation objects (e.g., `MatchDto` embeds `PlayerDto`).
  - Update DTOs omit `Id` (comes from the route).
- **Mappings** (`/Mappings/MappingExtensions.cs`) — extension methods (`entity.ToDto()`, `dto.UpdateEntity(entity)`) are the only place entity↔DTO conversion happens.
- When adding a record parameter to an existing DTO, **add it at the end** to avoid breaking positional construction.

### Controllers
- Controllers use DTOs for all input/output — never expose entities directly.
- Management endpoints (`/api/management/...`) require `[Authorize(Policy = "AdminOnly")]` (Keycloak `admin` realm role).

### Caching
Redis cache is injected as `ICacheService` / `RedisCacheService`.

### OpenAPI
Scalar API reference is available at `/scalar/v1` in development.

## Client Conventions

### API calls
All fetch logic lives in `src/services/api.ts`. Authenticated calls (management) pass `Authorization: Bearer {token}` explicitly. The base URL resolves from `VITE_API_BASE_URL` (Aspire-injected) or falls back to `http://localhost:5167`.

### Auth
`src/services/authService.ts` is a singleton class that manages token storage in `localStorage` (keys: `auth_token`, `auth_refresh_token`, `auth_username`, `auth_isAdmin`) and handles silent refresh (refreshes 5 minutes before expiry).

The auth flow is OIDC Authorization Code + PKCE, brokered by the **server** — the frontend redirects to `/api/auth/login`, Keycloak redirects back to `/api/auth/callback`, and the server bounces to the frontend with tokens as URL query params.

### Routing
React Router v7 via `Root.tsx`. `ProtectedRoute.tsx` gates the `/management` path.

## Aspire AppHost Notes

- SQL Server, Keycloak, and Redis all use **persistent containers and data volumes** — data survives restarts.
- Keycloak realm config is imported from `data/keycloak/` on first start.
- Dev Proxy (`devproxy`) intercepts `https://en.wikipedia.org/*` and starts explicitly (not auto-started).
- The AppHost also uses `AddDockerComposeEnvironment` — `PublishAsDockerComposeService` overrides are configured for production compose output.
- When adding new Aspire integrations: check current versions with the **list integrations** MCP tool; fetch docs with the **get integration docs** tool.

## Debugging

Use Aspire MCP diagnostic tools before modifying code:
1. **List structured logs** — structured app logs
2. **List console logs** — stdout per resource
3. **List traces** — distributed traces
4. **List trace structured logs** — logs scoped to a trace

Use Playwright MCP to do functional browser investigation against running resources (get endpoints from **list resources**).
