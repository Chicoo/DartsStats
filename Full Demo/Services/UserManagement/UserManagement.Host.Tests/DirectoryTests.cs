using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using UserManagement;

public class DirectoryTests
{
    [Fact]
    public async Task AdministrativeWritesAreNeverRetriedAndErrorsAreMapped()
    {
        var handler = new Handler((method, path, body) => path.Contains("/token")
            ? Response(new { access_token = "service-token", expires_in = 300 })
            : new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        var transport = Transport(handler);
        var error = await Assert.ThrowsAsync<DirectoryException>(() => transport.SendAsync(HttpMethod.Post, "users", new { username = "test" }, default));
        Assert.Equal(503, error.Status);
        Assert.Single(handler.Requests, r => !r.Path.Contains("/token"));
    }

    [Fact]
    public async Task RoleChangesPreserveUnrelatedRealmRolesAndRejectSystemRoles()
    {
        var handler = new Handler((method, path, body) =>
        {
            if (path.Contains("/token")) return Response(new { access_token = "service-token", expires_in = 300 });
            if (method == HttpMethod.Get && path.EndsWith("/users/id")) return Response(new { id = "id", username = "test" });
            if (method == HttpMethod.Get && path.EndsWith("/role-mappings/realm"))
                return Response(new[] { new { id = "1", name = "admin" }, new { id = "2", name = "offline_access" } });
            if (path.EndsWith("/roles/user")) return Response(new { id = "3", name = "user" });
            return new HttpResponseMessage(HttpStatusCode.NoContent);
        });
        var directory = new KeycloakUserDirectory(Transport(handler));
        await directory.SetRolesAsync("id", ["user"], default);
        var deletion = Assert.Single(handler.Requests, r => r.Method == HttpMethod.Delete);
        Assert.Contains("admin", deletion.Body);
        Assert.DoesNotContain("offline_access", deletion.Body);
        var previousCount = handler.Requests.Count;
        await Assert.ThrowsAsync<DirectoryException>(() => directory.SetRolesAsync("id", ["realm-admin"], default));
        Assert.Equal(previousCount, handler.Requests.Count);
        Assert.Single(handler.Requests, r => r.Path.Contains("/token"));
    }

    [Fact]
    public async Task SessionValidationUsesTheExactSessionId()
    {
        var handler = new Handler((method, path, body) => path.Contains("/token")
            ? Response(new { access_token = "token", expires_in = 300 })
            : Response(new[] { new { id = "active-session" } }));
        var directory = new KeycloakUserDirectory(Transport(handler));
        Assert.True(await directory.IsSessionActiveAsync("user", "active-session", default));
        Assert.False(await directory.IsSessionActiveAsync("user", "revoked-session", default));
    }

    private static KeycloakTransport Transport(HttpMessageHandler handler) => new(new HttpClient(handler), new()
    { BaseUrl = "https://directory.example.test", AdminClientSecret = "test" });
    private static HttpResponseMessage Response(object body) => new(HttpStatusCode.OK) { Content = JsonContent.Create(body) };

    private class Handler(Func<HttpMethod, string, string, HttpResponseMessage> respond) : HttpMessageHandler
    {
        public List<(HttpMethod Method, string Path, string Body)> Requests { get; } = [];
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = request.Content is null ? "" : await request.Content.ReadAsStringAsync(cancellationToken);
            Requests.Add((request.Method, request.RequestUri!.AbsolutePath, body));
            return respond(request.Method, request.RequestUri.AbsolutePath, body);
        }
    }
}
