# Management Section and Keycloak Authentication

This document describes the management section and Keycloak authentication setup for the DartsStats application.

## Overview

The application now includes:
- A management section for administrators to edit match data
- JWT-based authentication using Keycloak with OIDC/OAuth2 Authorization Code Flow with PKCE
- Role-based access control (admin role required)

## Features

### Management Section
- Update match statistics (averages, 180s, highest checkouts)
- Change match results (scores)
- Edit player assignments for matches
- Delete matches
- Protected by admin-only authorization

### Authentication
- Keycloak integration using OIDC/OAuth2 Authorization Code Flow with PKCE
- JWT Bearer token authentication
- Automatic token refresh
- Role-based authorization (admin role)

## Configuration

### Backend (ASP.NET Core)

The server requires Keycloak configuration in `appsettings.json`:

```json
{
  "Keycloak": {
    "Authority": "http://localhost:8089/realms/dartsstats",
    "ClientId": "dartsstats-web"
  }
}
```

Or use environment variables:
- `Keycloak__Authority`: Keycloak realm URL (e.g., `http://localhost:8089/realms/dartsstats`)
- `Keycloak__ClientId`: Client ID for the application (e.g., `dartsstats-web`)

### Frontend (React)

The client only requires the API base URL (create `.env` file):

```env
VITE_API_BASE_URL=http://localhost:5167
```

## Keycloak Setup

### 1. Create Realm
1. Log into Keycloak Admin Console
2. Create a new realm named `dartsstats` (or your preferred name)

### 2. Create Client

#### Web Client (dartsstats-web)
1. Create a new client with ID `dartsstats-web`
2. Set Client Protocol to `openid-connect`
3. **Client authentication**: `Off` (this is a public client)
4. **Authorization**: `Off`
5. **Authentication flow**:
   - ✅ Standard flow (Authorization Code Flow)
   - ✅ Direct access grants (for token refresh)
   - ❌ Implicit flow (deprecated, not needed)
   - ❌ Service accounts roles (not needed for public client)
6. **Valid Redirect URIs**: 
   - `http://localhost:5167/api/auth/callback`
   - `https://your-production-domain.com/api/auth/callback`
7. **Valid post logout redirect URIs**: 
 - `http://localhost:5173/*`
   - `https://your-production-domain.com/*`
8. **Web Origins**: 
   - `http://localhost:5173`
   - `http://localhost:5167`
   - `+` (for CORS)
9. **Advanced Settings**:
   - Proof Key for Code Exchange (PKCE): `S256` (required)
10. Save the client

### 3. Configure Client Scopes for Roles

#### Create or Configure Roles Client Scope
1. Go to **Client scopes**
2. Either use existing `roles` scope or create new one:
   - **Name**: `roles`
   - **Type**: `Default`
- **Protocol**: `openid-connect`
3. Go to **Mappers** tab
4. Add **Realm Roles Mapper**:
   - **Mapper Type**: `User Realm Role`
   - **Name**: `realm-roles`
   - **Token Claim Name**: `roles`
   - **Claim JSON Type**: `String`
   - **Add to ID token**: `ON`
   - **Add to access token**: `ON`
   - **Add to userinfo**: `ON`
   - **Multivalued**: `ON`

#### Assign Client Scope to Your Client
1. Go to **Clients** → Select `dartsstats-web`
2. Go to **Client scopes** tab
3. Under **Assigned client scopes**, ensure `roles` is in **Default** scopes
4. If not, click **Add client scope** → select `roles` → choose **Default**

### 4. Create Admin Role
1. Go to **Realm Roles**
2. Create a new role named `admin`
3. Add description: "Administrator role for management access"

### 5. Create Users
1. Go to **Users** → **Add User**
2. Set username and email
3. Save the user
4. Go to **Credentials** tab → Set password (disable "Temporary" option)
5. Go to **Role mapping** tab → Click **Assign role** → Select `admin` role

## Authentication Flow

The application uses **Authorization Code Flow with PKCE** (Proof Key for Code Exchange):

1. User clicks "Admin Login" on the frontend
2. Frontend redirects to backend `/api/auth/login`
3. Backend generates:
   - Random `state` for CSRF protection
   - `code_verifier` (random 64-char string)
   - `code_challenge` (SHA-256 hash of code_verifier)
4. Backend redirects user to Keycloak authorization endpoint with:
   - `client_id`, `redirect_uri`, `scope`, `state`
   - `code_challenge` and `code_challenge_method=S256`
5. User authenticates with Keycloak (username/password)
6. Keycloak redirects back to `/api/auth/callback` with authorization `code` and `state`
7. Backend validates `state` to prevent CSRF
8. Backend exchanges `code` for tokens at Keycloak's token endpoint using:
   - Authorization code
   - `code_verifier` (proves the requester is the same)
9. Keycloak validates PKCE and returns:
   - `access_token` (JWT)
   - `refresh_token`
   - Token expiration info
10. Backend parses JWT to extract:
    - Username (`preferred_username` claim)
    - Roles (from `roles`, `realm_access.roles`, or `resource_access` claims)
11. Backend redirects to frontend with token data as URL parameters
12. Frontend stores tokens and uses access token for authenticated API requests
13. Token is validated by backend on each request

This approach:
- **More secure than ROPC**: User credentials never pass through the application
- **PKCE protection**: Prevents authorization code interception attacks
- **CSRF protection**: State parameter prevents cross-site request forgery
- **Industry standard**: Recommended for modern web applications
- **User-friendly**: Standard web-based login flow

## API Endpoints

### Authentication Endpoints

#### Login (Initiates Authorization Flow)
```
GET /api/auth/login?returnUrl=/management

Response: 302 Redirect to Keycloak
```

#### Callback (Handles Authorization Response)
```
GET /api/auth/callback?code=xxx&state=yyy

Response: 302 Redirect to frontend with tokens
Example: /?token={jwt}&refreshToken={refresh}&username={user}&isAdmin=true&expiresIn=300
```

#### Logout
```
POST /api/auth/logout
Content-Type: application/json

{
  "refreshToken": "eyJhbGc..."
}

Response:
{
  "message": "Logged out successfully"
}
```

#### Refresh Token
```
POST /api/auth/refresh
Content-Type: application/json

{
  "refreshToken": "eyJhbGc..."
}

Response:
{
  "token": "eyJhbGc...",
  "refreshToken": "eyJhbGc...",
  "username": "admin",
  "isAdmin": true,
  "expiresIn": 300
}
```

### Management Endpoints (require admin role)

#### Get Match for Editing
```
GET /api/management/matches/{id}
Authorization: Bearer {token}
```

#### Update Match
```
PUT /api/management/matches/{id}
Authorization: Bearer {token}
Content-Type: application/json

{
  "player1Id": 1,
  "player2Id": 2,
  "matchDate": "2025-03-15T19:00:00Z",
  "player1Score": 6,
  "player2Score": 4,
  "player1Average": 98.5,
  "player2Average": 95.2,
  "player1180s": 3,
  "player2180s": 2,
  "player1HighestCheckout": 120,
  "player2HighestCheckout": 100,
  "season": "2025",
  "round": "Week 10"
}
```

#### Delete Match
```
DELETE /api/management/matches/{id}
Authorization: Bearer {token}
```

## Usage

### Accessing the Management Section

1. Navigate to the application homepage
2. Click "Admin Login" in the navigation bar
3. Browser redirects to Keycloak login page
4. Log in with your Keycloak credentials
5. After successful authentication, you'll be redirected back to the management section
6. If the user has the admin role, they can access all management features
7. Non-admin users will see an "Access Denied" message

### Editing Match Data

1. In the management section, click "Edit" on any match
2. Modify the desired fields:
   - Player selections
   - Match date
   - Scores
   - Statistics (averages, 180s, checkouts)
   - Season and round information
3. Click "Save Changes"
4. The match will be updated in the database

### Deleting Matches

1. Click "Delete" on any match
2. Confirm the deletion
3. The match will be permanently removed from the database

## Security

- **Authorization Code Flow with PKCE**: Most secure OAuth2 flow for public clients
- **CSRF Protection**: State parameter prevents cross-site request forgery
- **Code Challenge**: PKCE prevents authorization code interception
- All management endpoints require a valid JWT token
- Tokens must contain the `admin` role claim
- Token validation checks:
  - Issuer validation
  - Audience validation (if configured)
  - Signature validation
  - Expiration validation
- HTTPS is required in production
- CORS is configured to only allow the frontend origin

## Development

For development with Aspire, Keycloak is automatically configured:

```bash
# Start the application (from AppHost project)
dotnet run
```

Aspire will:
- Start Keycloak container on port 8089
- Import realm configuration from `../data/realm-export.json`
- Configure the API with correct Keycloak Authority URL

Manual Docker setup (if not using Aspire):

```bash
docker run -p 8089:8080 \
  -e KEYCLOAK_ADMIN=admin \
  -e KEYCLOAK_ADMIN_PASSWORD=admin \
  quay.io/keycloak/keycloak:latest start-dev
```

Then configure your environment variables to point to `http://localhost:8089/realms/dartsstats`.

## Troubleshooting

### "Invalid state parameter" error
- This is a CSRF protection mechanism
- Usually happens if you try to reuse an old callback URL
- Solution: Start a fresh login flow from `/api/auth/login`

### "Not authenticated" error
- Check that Keycloak is running and accessible at the configured Authority URL
- Verify the redirect URI in Keycloak client settings matches your backend URL
- Check browser console for CORS errors

### "Access Denied" message
- Verify the user has the `admin` role assigned in Keycloak
- Check the token contains the correct role claim
- Inspect the JWT token at https://jwt.io
- Check server logs for role extraction: `"User X logged in with roles: ..."`

### Token expiration
- Tokens automatically refresh in the background using the refresh token
- If refresh fails, the user will need to log in again
- Refresh tokens have longer expiration (typically 30 days)

### PKCE errors
- Ensure `code_challenge_method=S256` is supported in Keycloak client settings
- Check that Keycloak client has "Proof Key for Code Exchange Code Challenge Method" set to `S256`

### Roles not appearing in token
- Verify client scope `roles` is assigned as **Default** scope to your client
- Check that realm roles mapper is configured with `Multivalued=ON`
- Ensure `scope=openid profile email roles` is requested in login endpoint
- Test token at https://jwt.io to see what claims are included
