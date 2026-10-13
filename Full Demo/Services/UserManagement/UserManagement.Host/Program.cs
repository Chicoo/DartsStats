using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Scalar.AspNetCore;
using UserManagement;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
var options = builder.Configuration.GetSection("Keycloak").Get<KeycloakOptions>() ?? new();
builder.Services.AddSingleton(options);
// A dedicated client avoids the ServiceDefaults retries on administrative POST/PUT/DELETE.
builder.Services.AddSingleton(_ => new KeycloakTransport(new HttpClient { Timeout = TimeSpan.FromSeconds(30) }, options));
builder.Services.AddSingleton<IUserDirectory, KeycloakUserDirectory>();
builder.Services.AddSingleton<Provisioner>();

if (args.Contains("--provision"))
{
    using var host = builder.Build();
    await host.Services.GetRequiredService<Provisioner>().RunAsync(CancellationToken.None);
    host.Logger.LogInformation("User-management realm provisioning completed.");
    return;
}

if (string.IsNullOrWhiteSpace(options.BaseUrl) || string.IsNullOrWhiteSpace(options.PublicUrl)
    || string.IsNullOrWhiteSpace(options.ClientSecret) || string.IsNullOrWhiteSpace(options.AdminClientSecret))
    throw new InvalidOperationException("Keycloak BaseUrl, PublicUrl, ClientSecret and AdminClientSecret are required.");

builder.Services.AddControllersWithViews(o => o.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddAntiforgery(o => { o.HeaderName = "X-CSRF-TOKEN"; o.Cookie.Name = "um.csrf"; });
builder.Services.AddAuthentication(o =>
{
    o.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    o.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
}).AddCookie(o =>
{
    o.Cookie.Name = "um.session";
    o.Cookie.HttpOnly = true;
    o.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    o.Cookie.SameSite = SameSiteMode.Lax;
    o.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    o.SlidingExpiration = false;
    o.Events.OnRedirectToLogin = ctx => { ctx.Response.StatusCode = 401; return Task.CompletedTask; };
    o.Events.OnRedirectToAccessDenied = ctx => { ctx.Response.StatusCode = 403; return Task.CompletedTask; };
    o.Events.OnValidatePrincipal = async ctx =>
    {
        var id = ctx.Principal?.FindFirstValue("sub");
        if (id is null) { ctx.RejectPrincipal(); return; }
        try
        {
            var directory = ctx.HttpContext.RequestServices.GetRequiredService<IUserDirectory>();
            var user = await directory.GetAsync(id, ctx.HttpContext.RequestAborted);
            var sessionId = ctx.Principal?.FindFirstValue("sid");
            if (!user.Enabled || !user.Roles.Contains("admin") || sessionId is null
                || !await directory.IsSessionActiveAsync(id, sessionId, ctx.HttpContext.RequestAborted))
            { ctx.RejectPrincipal(); await ctx.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme); }
        }
        catch (DirectoryException ex) when (ex.Status == 404) { ctx.RejectPrincipal(); }
    };
}).AddOpenIdConnect(o =>
{
    o.Authority = options.PublicAuthority ?? options.Authority;
    o.ClientId = options.ClientId;
    o.ClientSecret = options.ClientSecret;
    o.ResponseType = OpenIdConnectResponseType.Code;
    o.UsePkce = true;
    o.SaveTokens = false;
    o.MapInboundClaims = false;
    o.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
    o.Scope.Clear();
    foreach (var scope in new[] { "openid", "profile", "email", "roles" }) o.Scope.Add(scope);
    o.TokenValidationParameters.NameClaimType = "preferred_username";
    o.TokenValidationParameters.RoleClaimType = "roles";
    o.Events.OnTokenValidated = ctx =>
    {
        var identity = (ClaimsIdentity)ctx.Principal!.Identity!;
        // Support both the managed direct mapper and Keycloak's standard realm role claim.
        var realmRoles = ctx.Principal.FindFirst("realm_access")?.Value;
        if (realmRoles is not null)
        {
            using var document = JsonDocument.Parse(realmRoles);
            if (document.RootElement.TryGetProperty("roles", out var roles))
                foreach (var role in roles.EnumerateArray())
                    if (KeycloakUserDirectory.SupportedRoles.Contains(role.GetString())) identity.AddClaim(new("roles", role.GetString()!));
        }
        if (!ctx.Principal.IsInRole("admin")) ctx.Fail("Administrator access is required.");
        return Task.CompletedTask;
    };
    o.Events.OnRemoteFailure = ctx =>
    { ctx.HandleResponse(); ctx.Response.Redirect(options.PublicUrl.TrimEnd('/') + "/?error=login_failed"); return Task.CompletedTask; };
});
builder.Services.AddAuthorization(o => o.AddPolicy("AdminOnly", p => p.RequireAuthenticatedUser().RequireRole("admin")));
builder.Services.AddHealthChecks().AddCheck<DirectoryHealthCheck>("user-directory");

var app = builder.Build();
app.UseExceptionHandler(handler => handler.Run(async ctx =>
{
    var error = ctx.Features.Get<IExceptionHandlerFeature>()?.Error;
    var status = error switch { DirectoryException de => de.Status, AntiforgeryValidationException => 400, _ => 500 };
    await Results.Problem(statusCode: status, title: error switch
    {
        DirectoryException => error.Message,
        AntiforgeryValidationException => "The request verification token is missing or invalid.",
        _ => "The request could not be completed."
    }).ExecuteAsync(ctx);
}));
// The configured public origin is authoritative; never trust arbitrary forwarded host headers.
app.Use(async (ctx, next) =>
{
    var origin = new Uri(options.PublicUrl);
    ctx.Request.Scheme = origin.Scheme;
    ctx.Request.Host = new HostString(origin.Authority);
    await next();
});
app.UseAuthentication();
app.UseAuthorization();
app.MapGet("/api/session", (HttpContext ctx, IAntiforgery antiforgery) => Results.Ok(new
{
    username = ctx.User.Identity!.Name, userId = ctx.User.FindFirstValue("sub"),
    csrfToken = antiforgery.GetAndStoreTokens(ctx).RequestToken, dartsUrl = options.DartsUrl
})).RequireAuthorization("AdminOnly");
app.MapGet("/api/auth/login", () => Results.Challenge(new AuthenticationProperties
{ RedirectUri = options.PublicUrl.TrimEnd('/') + "/" }, [OpenIdConnectDefaults.AuthenticationScheme]));
app.MapPost("/api/auth/logout", async (HttpContext ctx, IAntiforgery antiforgery) =>
{
    await antiforgery.ValidateRequestAsync(ctx);
    await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    // No tokens are needed to revoke this user's Keycloak SSO sessions.
    var userId = ctx.User.FindFirstValue("sub");
    if (userId is not null)
        await ctx.RequestServices.GetRequiredService<IUserDirectory>().RevokeSessionsAsync(userId, ctx.RequestAborted);
    return Results.NoContent();
}).RequireAuthorization("AdminOnly");
app.MapGet("/api/roles", () => KeycloakUserDirectory.SupportedRoles).RequireAuthorization("AdminOnly");
app.MapControllers();
if (app.Environment.IsDevelopment()) { app.MapOpenApi().RequireAuthorization("AdminOnly"); app.MapScalarApiReference().RequireAuthorization("AdminOnly"); }
app.MapHealthChecks("/health");
app.MapHealthChecks("/alive", new() { Predicate = c => c.Tags.Contains("live") });
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapFallbackToFile("index.html");
app.Run();

public partial class Program;

public sealed class DirectoryHealthCheck(KeycloakTransport transport) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try { await transport.SendAsync(HttpMethod.Get, "users/count", null, cancellationToken); return HealthCheckResult.Healthy(); }
        catch (Exception) { return HealthCheckResult.Unhealthy("The user directory is unavailable."); }
    }
}
