# User management

A separate ASP.NET Core service and React frontend wrapping the `dartsstats` Keycloak realm. The Darts API retains its existing authentication integration. User administration uses only the wrapper API.

## Run locally

From `Full Demo`, run `aspire run`. Open **user-management-web** in the dashboard, or <https://localhost:5178>.

Sign in as `demo-admin`. Its generated password is stored in the AppHost's user secrets as `Parameters:user-management-demo-admin-password`; view that secret in the Aspire dashboard. Client credentials are also generated and persisted as secret parameters. No credentials are committed.

Provisioning creates missing `admin`/`user` roles, dedicated OIDC and administration clients, and the initial demo administrator. It updates only its own client configuration, never resets existing account passwords, and does not promote an existing `demo-admin`. Existing realm data and unrelated roles are preserved. Demo-admin creation runs only in local Aspire run mode.

The frontend supports paginated search, account creation/profile edits, enable/disable, deletion, application-role assignment, and temporary-password resets. Usernames cannot be changed. Admins cannot disable/delete themselves or remove their own admin role. Temporary passwords must be 8–128 characters and satisfy the realm policy. Share them securely; users must change them through Keycloak on next login. Email delivery and self-service registration are not included.

## Authentication and API

The browser uses an HttpOnly, Secure, 30-minute session cookie. OIDC tokens and client secrets remain on the backend. Every authenticated request rechecks account status, the application admin role, and the live Keycloak session. Disabling an account, removing its admin role, or resetting its password therefore invalidates existing management access. Sign-out clears the local session and revokes the user's Keycloak SSO sessions. Administrative writes require `X-CSRF-TOKEN`, obtained from `GET /api/session`. An unavailable directory fails closed.

`GET /api/users?search=&page=1&pageSize=20`, `GET /api/users/{id}`, `POST /api/users`, `PUT /api/users/{id}`, `DELETE /api/users/{id}`, `PUT /api/users/{id}/enabled`, `PUT /api/users/{id}/roles`, `POST /api/users/{id}/password-reset`, and `GET /api/roles` are administrator-only. Development OpenAPI/Scalar documentation is available on the backend. Responses use application DTOs and Problem Details. Role changes preserve unrelated Keycloak roles. Administrative requests are not automatically retried; refresh the list after an uncertain result before retrying a mutation.

The service account has realm-management `query-users`, `view-users`, `manage-users`, and `view-realm`; the wrapper restricts assignable application roles to `admin` and `user`. Bootstrap administrator credentials are injected only into the provisioning job.

## Validation

```powershell
dotnet test Services/UserManagement/UserManagement.Host.Tests/UserManagement.Host.Tests.csproj
npm ci --prefix Services/UserManagement/UserManagement.Web
npm run build --prefix Services/UserManagement/UserManagement.Web
python Services/UserManagement/tests/smoke.py
```

The smoke test requires the local Aspire app, reads the demo-admin secret without printing it, and creates/deletes only a uniquely named test account. It verifies real OIDC login, CRUD, search, role changes, password-reset enforcement, CSRF, self-protection, and logout. Python HTTPS verification remains enabled.

## Publishing

The backend's Docker image includes the built frontend, served at the same origin. The Vite development resource is excluded from publishing. Set `Parameters:user-management-public-url` to the externally reachable HTTPS origin when publishing and configure the secret parameters. Set `Keycloak__PublicAuthority` on the deployed backend to the trusted, browser-reachable HTTPS realm URL (for example `https://identity.example.com/realms/dartsstats`); `Keycloak__BaseUrl` is the internal Admin REST address. Set `Keycloak__DartsUrl` to the public Darts frontend URL. Supply external routing/TLS and persistent ASP.NET Core data-protection keys for a durable deployment. Local development certificates are exported into the OS temporary directory by Vite, never into the repository.

Provisioning runs before the backend, including for an already-populated realm. Published deployments do not seed a demo account; provision an application administrator separately. The Full Demo's existing `start-dev` Keycloak configuration remains a demo deployment model.
