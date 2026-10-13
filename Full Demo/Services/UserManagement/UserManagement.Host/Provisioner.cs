using System.Text.Json.Nodes;

namespace UserManagement;

public sealed class Provisioner(KeycloakTransport transport, KeycloakOptions options, IConfiguration config)
{
    public async Task RunAsync(CancellationToken ct)
    {
        // Compose dependencies wait for container startup, not Keycloak readiness.
        // Retry only bootstrap authentication, before making administrative changes.
        for (var attempt = 0; ; attempt++)
        {
            try
            {
                await transport.UseBootstrapCredentialsAsync(Required("Bootstrap:Username"), Required("Bootstrap:Password"), ct);
                break;
            }
            catch (Exception ex) when (attempt < 30 && !ct.IsCancellationRequested &&
                ex is DirectoryException or HttpRequestException or TaskCanceledException)
            { await Task.Delay(TimeSpan.FromSeconds(2), ct); }
        }
        foreach (var role in KeycloakUserDirectory.SupportedRoles)
        {
            try { await transport.SendAsync(HttpMethod.Get, $"roles/{role}", null, ct); }
            catch (DirectoryException ex) when (ex.Status == 404)
            { await transport.SendAsync(HttpMethod.Post, "roles", new { name = role }, ct); }
        }

        await EnsureClientAsync(options.ClientId, new JsonObject
        {
            ["clientId"] = options.ClientId, ["protocol"] = "openid-connect", ["enabled"] = true,
            ["publicClient"] = false, ["secret"] = options.ClientSecret,
            ["standardFlowEnabled"] = true, ["directAccessGrantsEnabled"] = false,
            ["serviceAccountsEnabled"] = false, ["fullScopeAllowed"] = false,
            ["redirectUris"] = new JsonArray($"{options.PublicUrl.TrimEnd('/')}/signin-oidc"),
            ["webOrigins"] = new JsonArray(options.PublicUrl.TrimEnd('/')),
            ["attributes"] = new JsonObject { ["pkce.code.challenge.method"] = "S256",
                ["post.logout.redirect.uris"] = options.PublicUrl.TrimEnd('/') + "/*" },
            ["defaultClientScopes"] = new JsonArray("profile", "email", "roles")
        }, ct);
        var webClient = await ClientAsync(options.ClientId, ct);
        var webId = webClient["id"]!.GetValue<string>();
        var applicationRoles = new List<JsonNode>();
        foreach (var role in KeycloakUserDirectory.SupportedRoles)
            applicationRoles.Add((await transport.SendAsync(HttpMethod.Get, $"roles/{role}", null, ct))!);
        await transport.SendAsync(HttpMethod.Post, $"clients/{webId}/scope-mappings/realm", applicationRoles, ct);
        await EnsureMapperAsync(webId, "application-roles", new
        {
            name = "application-roles", protocol = "openid-connect", protocolMapper = "oidc-usermodel-realm-role-mapper",
            config = new Dictionary<string, string> { ["claim.name"] = "roles", ["jsonType.label"] = "String",
                ["multivalued"] = "true", ["id.token.claim"] = "true", ["access.token.claim"] = "true" }
        }, ct);
        await EnsureMapperAsync(webId, "management-audience", new
        {
            name = "management-audience", protocol = "openid-connect", protocolMapper = "oidc-audience-mapper",
            config = new Dictionary<string, string> { ["included.client.audience"] = options.ClientId,
                ["id.token.claim"] = "true", ["access.token.claim"] = "true" }
        }, ct);

        await EnsureClientAsync(options.AdminClientId, new JsonObject
        {
            ["clientId"] = options.AdminClientId, ["protocol"] = "openid-connect", ["enabled"] = true,
            ["publicClient"] = false, ["secret"] = options.AdminClientSecret,
            ["standardFlowEnabled"] = false, ["directAccessGrantsEnabled"] = false,
            ["serviceAccountsEnabled"] = true, ["fullScopeAllowed"] = true
        }, ct);
        var adminClient = await ClientAsync(options.AdminClientId, ct);
        var serviceUser = (await transport.SendAsync(HttpMethod.Get,
            $"clients/{adminClient["id"]!.GetValue<string>()}/service-account-user", null, ct))!;
        var managementClient = await ClientAsync("realm-management", ct);
        var managementId = managementClient["id"]!.GetValue<string>();
        var permissions = new List<JsonNode>();
        foreach (var role in new[] { "query-users", "view-users", "manage-users", "view-realm" })
            permissions.Add((await transport.SendAsync(HttpMethod.Get, $"clients/{managementId}/roles/{role}", null, ct))!);
        await transport.SendAsync(HttpMethod.Post,
            $"users/{serviceUser["id"]!.GetValue<string>()}/role-mappings/clients/{managementId}", permissions, ct);

        if (config.GetValue<bool>("Bootstrap:SeedAdmin"))
        {
            var username = config["Bootstrap:DemoAdminUsername"] ?? "demo-admin";
            var users = (await transport.SendAsync(HttpMethod.Get,
                $"users?username={Uri.EscapeDataString(username)}&exact=true", null, ct))!.AsArray();
            if (users.Count == 0)
            {
                await transport.SendAsync(HttpMethod.Post, "users", new
                {
                    username, enabled = true, firstName = "Demo", lastName = "Administrator",
                    email = "demo-admin@example.test", emailVerified = true,
                    credentials = new[] { new { type = "password", value = Required("Bootstrap:DemoAdminPassword"), temporary = false } }
                }, ct);
                users = (await transport.SendAsync(HttpMethod.Get,
                    $"users?username={Uri.EscapeDataString(username)}&exact=true", null, ct))!.AsArray();
                await transport.SendAsync(HttpMethod.Post,
                    $"users/{users.Single()!["id"]!.GetValue<string>()}/role-mappings/realm", applicationRoles, ct);
            }
            // Existing accounts are deliberately not promoted or given new passwords.
        }
    }

    private string Required(string key) => !string.IsNullOrWhiteSpace(config[key]) ? config[key]!
        : throw new InvalidOperationException($"Missing configuration: {key}");
    private async Task<JsonObject> ClientAsync(string id, CancellationToken ct) =>
        (await transport.SendAsync(HttpMethod.Get, $"clients?clientId={Uri.EscapeDataString(id)}", null, ct))!.AsArray().Single()!.AsObject();

    private async Task EnsureClientAsync(string id, JsonObject managed, CancellationToken ct)
    {
        var existing = (await transport.SendAsync(HttpMethod.Get,
            $"clients?clientId={Uri.EscapeDataString(id)}", null, ct))!.AsArray();
        if (existing.Count == 0) await transport.SendAsync(HttpMethod.Post, "clients", managed, ct);
        else
        {
            var current = existing.Single()!.AsObject();
            foreach (var field in managed) current[field.Key] = field.Value?.DeepClone();
            await transport.SendAsync(HttpMethod.Put, $"clients/{current["id"]!.GetValue<string>()}", current, ct);
        }
    }

    private async Task EnsureMapperAsync(string clientId, string name, object body, CancellationToken ct)
    {
        var path = $"clients/{clientId}/protocol-mappers/models";
        var mappers = (await transport.SendAsync(HttpMethod.Get, path, null, ct))!.AsArray();
        var existing = mappers.FirstOrDefault(m => m!["name"]!.GetValue<string>() == name);
        if (existing is null) await transport.SendAsync(HttpMethod.Post, path, body, ct);
        else
        {
            var mapper = System.Text.Json.JsonSerializer.SerializeToNode(body)!.AsObject();
            mapper["id"] = existing["id"]!.DeepClone();
            await transport.SendAsync(HttpMethod.Put, $"{path}/{existing["id"]!.GetValue<string>()}", mapper, ct);
        }
    }
}
