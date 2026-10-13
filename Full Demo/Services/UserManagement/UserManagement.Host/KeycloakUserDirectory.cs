using System.Text.Json.Nodes;

namespace UserManagement;

public sealed class KeycloakUserDirectory(KeycloakTransport transport) : IUserDirectory
{
    public static readonly string[] SupportedRoles = ["admin", "user"];
    private static string UserPath(string id) => $"users/{Uri.EscapeDataString(id)}";
    private async Task<JsonObject> RawUserAsync(string id, CancellationToken ct) =>
        (await transport.SendAsync(HttpMethod.Get, UserPath(id), null, ct))!.AsObject();

    private async Task<UserDto> MapAsync(JsonObject user, CancellationToken ct)
    {
        var id = user["id"]!.GetValue<string>();
        var roles = (await transport.SendAsync(HttpMethod.Get, $"{UserPath(id)}/role-mappings/realm", null, ct))!.AsArray();
        return new(id, user["username"]!.GetValue<string>(), user["firstName"]?.GetValue<string>(),
            user["lastName"]?.GetValue<string>(), user["email"]?.GetValue<string>(),
            user["enabled"]?.GetValue<bool>() ?? false,
            roles.Select(r => r!["name"]!.GetValue<string>()).Where(SupportedRoles.Contains).ToArray());
    }

    public async Task<UserPage> ListAsync(string? search, int page, int pageSize, CancellationToken ct)
    {
        var query = string.IsNullOrWhiteSpace(search) ? "" : $"&search={Uri.EscapeDataString(search.Trim())}";
        var users = (await transport.SendAsync(HttpMethod.Get,
            $"users?first={(page - 1) * pageSize}&max={pageSize}{query}", null, ct))!.AsArray();
        var count = (await transport.SendAsync(HttpMethod.Get, $"users/count?{query.TrimStart('&')}", null, ct))!.GetValue<int>();
        var mapped = new List<UserDto>();
        foreach (var user in users) mapped.Add(await MapAsync(user!.AsObject(), ct));
        return new(mapped.ToArray(), count, page, pageSize);
    }

    public async Task<UserDto> GetAsync(string id, CancellationToken ct) => await MapAsync(await RawUserAsync(id, ct), ct);

    public async Task<UserDto> CreateAsync(CreateUserRequest user, CancellationToken ct)
    {
        await transport.SendAsync(HttpMethod.Post, "users", new
        {
            username = user.Username.Trim(), firstName = user.FirstName, lastName = user.LastName,
            email = user.Email, enabled = user.Enabled,
            realmRoles = new[] { "user" },
            credentials = new[] { new { type = "password", value = user.TemporaryPassword, temporary = true } }
        }, ct);
        var created = (await transport.SendAsync(HttpMethod.Get,
            $"users?username={Uri.EscapeDataString(user.Username.Trim())}&exact=true", null, ct))!.AsArray().Single()!.AsObject();
        var id = created["id"]!.GetValue<string>();
        // Admin REST create does not reliably apply realmRoles; assign explicitly.
        await SetRolesAsync(id, ["user"], ct);
        return await GetAsync(id, ct);
    }

    public async Task UpdateAsync(string id, UpdateUserRequest user, CancellationToken ct)
    {
        var existing = await RawUserAsync(id, ct);
        existing["firstName"] = user.FirstName;
        existing["lastName"] = user.LastName;
        if (existing["email"]?.GetValue<string>() != user.Email) existing["emailVerified"] = false;
        existing["email"] = user.Email;
        await transport.SendAsync(HttpMethod.Put, UserPath(id), existing, ct);
    }

    public async Task SetEnabledAsync(string id, bool enabled, CancellationToken ct)
    {
        var user = await RawUserAsync(id, ct);
        user["enabled"] = enabled;
        await transport.SendAsync(HttpMethod.Put, UserPath(id), user, ct);
        if (!enabled) await transport.SendAsync(HttpMethod.Post, $"{UserPath(id)}/logout", null, ct);
    }
    public async Task DeleteAsync(string id, CancellationToken ct) =>
        await transport.SendAsync(HttpMethod.Delete, UserPath(id), null, ct);

    public async Task SetRolesAsync(string id, string[] roles, CancellationToken ct)
    {
        if (roles.Any(r => !SupportedRoles.Contains(r)))
            throw new DirectoryException(400, "Only the admin and user application roles can be assigned.");
        await RawUserAsync(id, ct);
        var path = $"{UserPath(id)}/role-mappings/realm";
        var current = (await transport.SendAsync(HttpMethod.Get, path, null, ct))!.AsArray();
        var remove = current.Where(r => SupportedRoles.Contains(r!["name"]!.GetValue<string>())
            && !roles.Contains(r!["name"]!.GetValue<string>())).Select(r => r!.DeepClone()).ToArray();
        if (remove.Length > 0) await transport.SendAsync(HttpMethod.Delete, path, remove, ct);
        var add = new List<JsonNode>();
        foreach (var role in roles.Distinct())
            if (!current.Any(r => r!["name"]!.GetValue<string>() == role))
                add.Add((await transport.SendAsync(HttpMethod.Get, $"roles/{role}", null, ct))!);
        if (add.Count > 0) await transport.SendAsync(HttpMethod.Post, path, add, ct);
    }

    public async Task ResetPasswordAsync(string id, string password, CancellationToken ct)
    {
        await transport.SendAsync(HttpMethod.Put, $"{UserPath(id)}/reset-password",
            new { type = "password", value = password, temporary = true }, ct);
        await transport.SendAsync(HttpMethod.Post, $"{UserPath(id)}/logout", null, ct);
    }
    public async Task RevokeSessionsAsync(string id, CancellationToken ct) =>
        await transport.SendAsync(HttpMethod.Post, $"{UserPath(id)}/logout", null, ct);
    public async Task<bool> IsSessionActiveAsync(string id, string sessionId, CancellationToken ct)
    {
        var sessions = (await transport.SendAsync(HttpMethod.Get, $"{UserPath(id)}/sessions", null, ct))!.AsArray();
        return sessions.Any(session => session?["id"]?.GetValue<string>() == sessionId);
    }
}
