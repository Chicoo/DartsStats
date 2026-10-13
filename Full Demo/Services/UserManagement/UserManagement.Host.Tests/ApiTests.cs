using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting;
using UserManagement;

public class ApiTests
{
    [Fact]
    public async Task AnonymousRequestsAre401AndDoNotRedirectToKeycloak()
    {
        using var factory = new Factory();
        using var client = factory.CreateClient(new() { AllowAutoRedirect = false });
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/session")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/users")).StatusCode);
    }

    [Fact]
    public async Task NonAdminCannotReadOrMutate()
    {
        using var factory = new Factory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("Test-Role", "user");
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/users")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.DeleteAsync("/api/users/other")).StatusCode);
    }

    [Fact]
    public async Task AdminCrudRequiresCsrfAndValidatesInputs()
    {
        using var factory = new Factory();
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        client.DefaultRequestHeaders.Add("Test-Role", "admin");
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/users")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.DeleteAsync("/api/users/other")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync("/api/auth/logout", null)).StatusCode);
        var session = await client.GetFromJsonAsync<System.Text.Json.Nodes.JsonObject>("/api/session");
        client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", session!["csrfToken"]!.GetValue<string>());
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/users?pageSize=101")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.DeleteAsync("/api/users/self")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/users/other/password-reset", new { temporaryPassword = "short" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync("/api/users/other/roles", new { roles = new[] { "realm-admin" } })).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/api/users", new CreateUserRequest("new", "New", "User", "new@example.test", true, "Temporary123!"))).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PutAsJsonAsync("/api/users/other", new UpdateUserRequest("Edited", null, null))).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PutAsJsonAsync("/api/users/other/enabled", new EnabledRequest(false))).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PutAsJsonAsync("/api/users/other/roles", new RolesRequest(["admin", "user"]))).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync("/api/users/other/password-reset", new PasswordRequest("Temporary456!"))).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync("/api/users/other")).StatusCode);
    }

    [Theory]
    [InlineData("missing", 404)]
    [InlineData("offline", 503)]
    [InlineData("duplicate", 409)]
    public async Task DirectoryErrorsBecomeProblemDetails(string id, int status)
    {
        using var factory = new Factory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("Test-Role", "admin");
        var response = await client.GetAsync($"/api/users/{id}");
        Assert.Equal(status, (int)response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }
}

public class Factory : WebApplicationFactory<Program>
{
    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureHostConfiguration(config => config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Keycloak:BaseUrl"] = "https://directory.example.test", ["Keycloak:PublicUrl"] = "https://localhost",
            ["Keycloak:ClientSecret"] = "test", ["Keycloak:AdminClientSecret"] = "test"
        }));
        return base.CreateHost(builder);
    }
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Keycloak:BaseUrl"] = "https://directory.example.test", ["Keycloak:PublicUrl"] = "https://localhost",
            ["Keycloak:ClientSecret"] = "test", ["Keycloak:AdminClientSecret"] = "test"
        }));
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IUserDirectory>();
            services.AddSingleton<IUserDirectory, FakeDirectory>();
            services.AddAuthentication(o => { o.DefaultScheme = "Test"; o.DefaultChallengeScheme = "Test"; })
                .AddScheme<AuthenticationSchemeOptions, TestAuth>("Test", _ => { });
        });
    }
}

public class TestAuth(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var role = Request.Headers["Test-Role"].ToString();
        if (role.Length == 0) return Task.FromResult(AuthenticateResult.NoResult());
        var identity = new ClaimsIdentity([new("sub", "self"), new(ClaimTypes.Name, "tester"), new(ClaimTypes.Role, role)], "Test");
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), "Test")));
    }
}

public class FakeDirectory : IUserDirectory
{
    public Task<UserPage> ListAsync(string? search, int page, int pageSize, CancellationToken ct) => Task.FromResult(new UserPage([], 0, page, pageSize));
    public Task<UserDto> GetAsync(string id, CancellationToken ct) => id switch
    {
        "missing" => throw new DirectoryException(404, "Missing user"),
        "offline" => throw new DirectoryException(503, "Directory unavailable"),
        "duplicate" => throw new DirectoryException(409, "Duplicate user"),
        _ => Task.FromResult(new UserDto(id, "tester", null, null, null, true, ["admin"]))
    };
    public Task<UserDto> CreateAsync(CreateUserRequest user, CancellationToken ct) => GetAsync("new", ct);
    public Task UpdateAsync(string id, UpdateUserRequest user, CancellationToken ct) => Task.CompletedTask;
    public Task SetEnabledAsync(string id, bool enabled, CancellationToken ct) => Task.CompletedTask;
    public Task DeleteAsync(string id, CancellationToken ct) => Task.CompletedTask;
    public Task SetRolesAsync(string id, string[] roles, CancellationToken ct) => roles.Any(r => !KeycloakUserDirectory.SupportedRoles.Contains(r))
        ? throw new DirectoryException(400, "Invalid roles") : Task.CompletedTask;
    public Task ResetPasswordAsync(string id, string password, CancellationToken ct) => Task.CompletedTask;
    public Task RevokeSessionsAsync(string id, CancellationToken ct) => Task.CompletedTask;
    public Task<bool> IsSessionActiveAsync(string id, string sessionId, CancellationToken ct) => Task.FromResult(true);
}
