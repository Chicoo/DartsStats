using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace UserManagement;

// One instance per process: serialize token renewal, and never retry an administrative mutation.
public sealed class KeycloakTransport(HttpClient http, KeycloakOptions options)
{
    private readonly SemaphoreSlim tokenLock = new(1, 1);
    private string? token;
    private DateTimeOffset expiresAt;

    public async Task UseBootstrapCredentialsAsync(string username, string password, CancellationToken ct)
    {
        await FetchTokenAsync($"{options.BaseUrl.TrimEnd('/')}/realms/master/protocol/openid-connect/token",
            new() { ["grant_type"] = "password", ["client_id"] = "admin-cli", ["username"] = username, ["password"] = password }, ct);
    }

    private async Task FetchTokenAsync(string endpoint, Dictionary<string, string> form, CancellationToken ct)
    {
        using var response = await http.PostAsync(endpoint, new FormUrlEncodedContent(form), ct);
        if (!response.IsSuccessStatusCode)
            throw new DirectoryException(503, "The user directory could not authenticate. Check its service credentials.");
        var result = await response.Content.ReadFromJsonAsync<JsonObject>(ct)
            ?? throw new DirectoryException(503, "Invalid directory authentication response.");
        token = result["access_token"]!.GetValue<string>();
        expiresAt = DateTimeOffset.UtcNow.AddSeconds(result["expires_in"]!.GetValue<int>() - 30);
    }

    private async Task<string> GetTokenAsync(CancellationToken ct)
    {
        await tokenLock.WaitAsync(ct);
        try
        {
            if (token is null || DateTimeOffset.UtcNow >= expiresAt)
                await FetchTokenAsync($"{options.Authority}/protocol/openid-connect/token", new()
                {
                    ["grant_type"] = "client_credentials", ["client_id"] = options.AdminClientId,
                    ["client_secret"] = options.AdminClientSecret
                }, ct);
            return token!;
        }
        finally { tokenLock.Release(); }
    }

    public async Task<JsonNode?> SendAsync(HttpMethod method, string path, object? body, CancellationToken ct)
    {
        try
        {
            using var request = new HttpRequestMessage(method,
                $"{options.BaseUrl.TrimEnd('/')}/admin/realms/{Uri.EscapeDataString(options.Realm)}/{path}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await GetTokenAsync(ct));
            if (body is not null) request.Content = JsonContent.Create(body);
            using var response = await http.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
                throw new DirectoryException(response.StatusCode switch
                {
                    HttpStatusCode.NotFound => 404,
                    HttpStatusCode.Conflict => 409,
                    HttpStatusCode.BadRequest => 400,
                    _ => 503
                }, response.StatusCode switch
                {
                    HttpStatusCode.NotFound => "The requested user or role was not found.",
                    HttpStatusCode.Conflict => "The username or email is already in use.",
                    HttpStatusCode.BadRequest => "The directory rejected this change. Check the fields and password policy.",
                    _ => "The user directory is unavailable or its service permissions are misconfigured."
                });
            if (response.StatusCode == HttpStatusCode.NoContent || response.Content.Headers.ContentLength == 0)
                return null;
            var content = await response.Content.ReadAsStringAsync(ct);
            return string.IsNullOrWhiteSpace(content) ? null : JsonNode.Parse(content);
        }
        catch (HttpRequestException) { throw new DirectoryException(503, "The user directory is unavailable."); }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        { throw new DirectoryException(503, "The user directory request timed out."); }
    }
}
