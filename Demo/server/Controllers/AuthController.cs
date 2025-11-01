using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;
using System.Security.Cryptography;

namespace DartsStats.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthController> _logger;
        private readonly HttpClient _httpClient;
        private static readonly Dictionary<string, string> _stateStore = new();

        public AuthController(IConfiguration configuration, ILogger<AuthController> logger, IHttpClientFactory httpClientFactory)
        {
            _configuration = configuration;
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient();
        }

        /// <summary>
        /// Initiates the OIDC authorization code flow by redirecting to Keycloak
        /// </summary>
        [HttpGet("login")]
        public IActionResult Login([FromQuery] string? returnUrl = null)
        {
            var keycloakAuthority = _configuration["Keycloak:Authority"];
            var keycloakClientId = _configuration["Keycloak:ClientId"];
            var redirectUri = $"{Request.Scheme}://{Request.Host}/api/auth/callback";

            // Generate random state for CSRF protection
            var state = GenerateRandomString(32);
            var codeVerifier = GenerateRandomString(64);
            var codeChallenge = GenerateCodeChallenge(codeVerifier);

            // Store state and code verifier for validation in callback
            _stateStore[state] = $"{codeVerifier}|{returnUrl ?? "/"}";

            var authorizationEndpoint = $"{keycloakAuthority}/protocol/openid-connect/auth";
            var queryParams = new Dictionary<string, string>
            {
                ["client_id"] = keycloakClientId!,
                ["response_type"] = "code",
                ["redirect_uri"] = redirectUri,
                ["scope"] = "openid profile email roles",
                ["state"] = state,
                ["code_challenge"] = codeChallenge,
                ["code_challenge_method"] = "S256"
            };

            var query = string.Join("&", queryParams.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
            var authUrl = $"{authorizationEndpoint}?{query}";

            return Redirect(authUrl);
        }

        /// <summary>
        /// Handles the callback from Keycloak after user authentication
        /// </summary>
        [HttpGet("callback")]
        public async Task<IActionResult> Callback([FromQuery] string code, [FromQuery] string state)
        {
            try
            {
                // Validate state
                if (!_stateStore.TryGetValue(state, out var storedData))
                {
                    return BadRequest("Invalid state parameter");
                }

                var parts = storedData.Split('|');
                var codeVerifier = parts[0];
                var returnUrl = parts.Length > 1 ? parts[1] : "/";
                _stateStore.Remove(state);

                var keycloakAuthority = _configuration["Keycloak:Authority"];
                var keycloakClientId = _configuration["Keycloak:ClientId"];
                var redirectUri = $"{Request.Scheme}://{Request.Host}/api/auth/callback";
                var tokenEndpoint = $"{keycloakAuthority}/protocol/openid-connect/token";

                // Exchange authorization code for tokens
                var tokenRequest = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("grant_type", "authorization_code"),
                    new KeyValuePair<string, string>("client_id", keycloakClientId!),
                    new KeyValuePair<string, string>("code", code),
                    new KeyValuePair<string, string>("redirect_uri", redirectUri),
                    new KeyValuePair<string, string>("code_verifier", codeVerifier)
                });

                var response = await _httpClient.PostAsync(tokenEndpoint, tokenRequest);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Keycloak token exchange failed: {Error}", errorContent);
                    return Redirect($"/?error=authentication_failed");
                }

                var tokenResponse = await response.Content.ReadAsStringAsync();
                var tokenData = JsonSerializer.Deserialize<KeycloakTokenResponse>(tokenResponse);

                if (tokenData?.AccessToken == null)
                {
                    _logger.LogError("Failed to parse Keycloak token response");
                    return Redirect($"/?error=authentication_failed");
                }

                // Parse the JWT to extract user info
                var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(tokenData.AccessToken);
                
                var username = jwtToken.Claims.FirstOrDefault(c => c.Type == "preferred_username")?.Value ?? "unknown";

                // Extract roles from Keycloak token
                var roles = ExtractRolesFromToken(jwtToken);
                var isAdmin = roles.Contains("admin", StringComparer.OrdinalIgnoreCase);

                _logger.LogInformation("User {Username} logged in successfully via Keycloak with roles: {Roles}", 
  username, string.Join(", ", roles));

                // Encode the token data as URL parameters to pass to frontend
                var tokenParam = Uri.EscapeDataString(tokenData.AccessToken);
                var refreshTokenParam = Uri.EscapeDataString(tokenData.RefreshToken ?? "");
                var usernameParam = Uri.EscapeDataString(username);
                var isAdminParam = isAdmin.ToString().ToLower();
                var expiresInParam = tokenData.ExpiresIn.ToString();

                // Redirect to frontend with token data
                return Redirect($"{returnUrl}?token={tokenParam}&refreshToken={refreshTokenParam}&username={usernameParam}&isAdmin={isAdminParam}&expiresIn={expiresInParam}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Keycloak authentication callback");
                return Redirect($"/?error=authentication_failed");
            }
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] LogoutRequest request)
        {
            try
            {
                var keycloakAuthority = _configuration["Keycloak:Authority"];
                var keycloakClientId = _configuration["Keycloak:ClientId"];
                var logoutEndpoint = $"{keycloakAuthority}/protocol/openid-connect/logout";

                if (!string.IsNullOrEmpty(request.RefreshToken))
                {
                    var logoutRequest = new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string>("client_id", keycloakClientId!),
                        new KeyValuePair<string, string>("refresh_token", request.RefreshToken)
                    });

                    await _httpClient.PostAsync(logoutEndpoint, logoutRequest);
                }

                return Ok(new { message = "Logged out successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Keycloak logout");
                return Ok(new { message = "Logged out successfully" }); // Still return success for client
            }
        }

        [HttpPost("refresh")]
        public async Task<ActionResult<LoginResponse>> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            try
            {
                var keycloakAuthority = _configuration["Keycloak:Authority"];
                var keycloakClientId = _configuration["Keycloak:ClientId"];
                var tokenEndpoint = $"{keycloakAuthority}/protocol/openid-connect/token";

                var tokenRequest = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("grant_type", "refresh_token"),
                    new KeyValuePair<string, string>("client_id", keycloakClientId!),
                    new KeyValuePair<string, string>("refresh_token", request.RefreshToken)
                });

                var response = await _httpClient.PostAsync(tokenEndpoint, tokenRequest);

                if (!response.IsSuccessStatusCode)
                {
                    return Unauthorized(new { message = "Token refresh failed" });
                }

                var tokenResponse = await response.Content.ReadAsStringAsync();
                var tokenData = JsonSerializer.Deserialize<KeycloakTokenResponse>(tokenResponse);

                if (tokenData?.AccessToken == null)
                {
                    return StatusCode(500, new { message = "Token refresh failed" });
                }

                var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(tokenData.AccessToken);
                
                var username = jwtToken.Claims.FirstOrDefault(c => c.Type == "preferred_username")?.Value ?? "";
      
                // Extract roles from Keycloak token
                var roles = ExtractRolesFromToken(jwtToken);
                var isAdmin = roles.Contains("admin", StringComparer.OrdinalIgnoreCase);

                return Ok(new LoginResponse
                {
                    Token = tokenData.AccessToken,
                    RefreshToken = tokenData.RefreshToken,
                    Username = username,
                    IsAdmin = isAdmin,
                    ExpiresIn = tokenData.ExpiresIn
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing token");
                return StatusCode(500, new { message = "Token refresh error" });
            }
        }

        /// <summary>
        /// Extracts roles from Keycloak JWT token
        /// Handles both realm_access.roles and direct roles claim
        /// </summary>
        private static List<string> ExtractRolesFromToken(System.IdentityModel.Tokens.Jwt.JwtSecurityToken jwtToken)
        {
            var roles = new List<string>();

            // Method 1: Check for direct "roles" claim (if mapper is configured)
            var rolesClaims = jwtToken.Claims.Where(c => c.Type == "roles").Select(c => c.Value);
            roles.AddRange(rolesClaims);

            // Method 2: Parse realm_access.roles from JSON
            var realmAccessClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "realm_access");
            if (realmAccessClaim != null)
            {
                try
                {
                    using var doc = JsonDocument.Parse(realmAccessClaim.Value);
                    if (doc.RootElement.TryGetProperty("roles", out var rolesArray))
                    {
                        foreach (var role in rolesArray.EnumerateArray())
                        {
                            var roleValue = role.GetString();
                            if (!string.IsNullOrEmpty(roleValue) && !roles.Contains(roleValue))
                            {
                                roles.Add(roleValue);
                            }
                        }
                    }
                }
                catch (JsonException ex)
                {
                    // Log but don't fail - realm_access might not be JSON
                    System.Diagnostics.Debug.WriteLine($"Failed to parse realm_access: {ex.Message}");
                }
            }

            // Method 3: Check resource_access for client-specific roles
            var resourceAccessClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "resource_access");
            if (resourceAccessClaim != null)
            {
                try
                {
                    using var doc = JsonDocument.Parse(resourceAccessClaim.Value);
                    foreach (var client in doc.RootElement.EnumerateObject())
                    {
                        if (client.Value.TryGetProperty("roles", out var rolesArray))
                        {
                            foreach (var role in rolesArray.EnumerateArray())
                            {
                                var roleValue = role.GetString();
                                if (!string.IsNullOrEmpty(roleValue) && !roles.Contains(roleValue))
                                {
                                    roles.Add(roleValue);
                                }
                            }
                        }
                    }
                }
                catch (JsonException ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to parse resource_access: {ex.Message}");
                }
            }

            return roles;
        }

        private static string GenerateRandomString(int length)
        {
            var bytes = new byte[length];
            RandomNumberGenerator.Fill(bytes);
            return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_')[..length];
        }

        private static string GenerateCodeChallenge(string codeVerifier)
        {
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(codeVerifier));
            return Convert.ToBase64String(hash).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }
    }

    public class LogoutRequest
    {
        public string RefreshToken { get; set; } = string.Empty;
    }

    public class RefreshTokenRequest
    {
        public string RefreshToken { get; set; } = string.Empty;
    }

    public class LoginResponse
    {
        public string Token { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public bool IsAdmin { get; set; }
        public int ExpiresIn { get; set; }
    }

    public class KeycloakTokenResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        [System.Text.Json.Serialization.JsonPropertyName("refresh_token")]
        public string RefreshToken { get; set; } = string.Empty;

        [System.Text.Json.Serialization.JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("refresh_expires_in")]
        public int RefreshExpiresIn { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("token_type")]
        public string TokenType { get; set; } = string.Empty;
    }
}
