# Management Section and Keycloak Authentication

This document describes the management section and Keycloak authentication setup for the DartsStats application.

## Overview

The application now includes:
- A management section for administrators to edit match data
- JWT-based authentication using Keycloak with OIDC/OAuth2
- Role-based access control (admin role required)

## Features

### Management Section
- Update match statistics (averages, 180s, highest checkouts)
- Change match results (scores)
- Edit player assignments for matches
- Delete matches
- Protected by admin-only authorization

### Authentication
- Keycloak integration using OIDC/OAuth2
- JWT Bearer token authentication
- Automatic token refresh
- Role-based authorization (admin role)

## Configuration

### Backend (ASP.NET Core)

The server requires Keycloak configuration in `appsettings.json`:

```json
{
  "Keycloak": {
    "Authority": "https://your-keycloak-server/realms/your-realm",
    "Audience": "dartsstats-api"
  }
}
```

Or use environment variables:
- `Keycloak__Authority`: Keycloak realm URL
- `Keycloak__Audience`: Client ID for API validation

### Frontend (React)

The client requires environment variables (create `.env` file):

```env
VITE_KEYCLOAK_AUTHORITY=https://your-keycloak-server/realms/your-realm
VITE_KEYCLOAK_CLIENT_ID=dartsstats-web
VITE_API_BASE_URL=http://localhost:5167
```

## Keycloak Setup

### 1. Create Realm
1. Log into Keycloak Admin Console
2. Create a new realm named `dartsstats` (or your preferred name)

### 2. Create Clients

#### API Client (dartsstats-api)
1. Create a new client with ID `dartsstats-api`
2. Set Client Protocol to `openid-connect`
3. Set Access Type to `bearer-only`
4. Save the client

#### Web Client (dartsstats-web)
1. Create a new client with ID `dartsstats-web`
2. Set Client Protocol to `openid-connect`
3. Set Access Type to `public`
4. Enable Standard Flow
5. Set Valid Redirect URIs: `http://localhost:5173/callback`, `http://localhost:5173/*`
6. Set Web Origins: `http://localhost:5173`
7. Save the client

### 3. Create Admin Role
1. Go to Realm Roles
2. Create a new role named `admin`
3. Add description: "Administrator role for management access"

### 4. Create Users
1. Go to Users → Add User
2. Set username and email
3. Save the user
4. Go to Credentials tab → Set password
5. Go to Role Mappings tab → Assign `admin` role

## API Endpoints

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
3. Log in with Keycloak credentials
4. If the user has the admin role, they will be redirected to the management section
5. Non-admin users will see an "Access Denied" message

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

- All management endpoints require a valid JWT token
- Tokens must contain the `admin` role claim
- Token validation checks:
  - Issuer validation
  - Audience validation
  - Signature validation
  - Expiration validation
- HTTPS is required in production
- CORS is configured to only allow the frontend origin

## Development

For development, you can use a local Keycloak instance:

```bash
docker run -p 8080:8080 -e KEYCLOAK_ADMIN=admin -e KEYCLOAK_ADMIN_PASSWORD=admin quay.io/keycloak/keycloak:latest start-dev
```

Then configure your environment variables to point to `http://localhost:8080/realms/dartsstats`.

## Troubleshooting

### "Not authenticated" error
- Check that Keycloak is running and accessible
- Verify the Authority URL in configuration
- Check browser console for CORS errors

### "Access Denied" message
- Verify the user has the `admin` role assigned
- Check the token contains the correct role claim
- Inspect the JWT token at https://jwt.io

### Token expiration
- Tokens automatically refresh in the background
- If refresh fails, the user will need to log in again
