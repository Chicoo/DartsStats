using k8s.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var builder = DistributedApplication.CreateBuilder(args);

var compose = builder.AddDockerComposeEnvironment("dartsstats-compose");
compose.Resource.DefaultNetworkName = "darts-stats";
compose.WithDashboard(configure =>
{
    configure.WithHostPort(18888);
});

var keycloak_username = builder.AddParameter("keycloak-username", "admin");
var keycloak_password = builder.AddParameter("keycloak-password", "admin");
if (!int.TryParse(builder.Configuration["Keycloak:Port"], out var keycloak_port))
{
    keycloak_port = 8080;
}

var sqlServer = builder.AddSqlServer("sqlserver")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDbGate()
    .WithDataVolume()
    .AddDatabase("dartsstats", "DartsStats");

var keycloak = builder.AddKeycloak("keycloak", keycloak_port, keycloak_username, keycloak_password)
    .WithImageTag("latest")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume()
    .WithRealmImport("../data/keycloak")
    .WithExternalHttpEndpoints()
    .PublishAsDockerComposeService((resource, service) =>
    {
        service.Command =
        [
            "start-dev",
            "--import-realm"
        ];
    });

keycloak_username.WithParentRelationship(keycloak);
keycloak_password.WithParentRelationship(keycloak);

var redis = builder.AddRedis("redis", port: 6380)
    .WithRedisInsight()
    .WithRedisCommander()
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume();

redis.WithCommand("clear-data", "Clear Redis Data", async context =>
{
    var connectionString = await redis.Resource.GetConnectionStringAsync();
    if (connectionString != null)
    {
        var configOptions = StackExchange.Redis.ConfigurationOptions.Parse(connectionString);
        configOptions.AllowAdmin = true;
        
        var connection = await StackExchange.Redis.ConnectionMultiplexer.ConnectAsync(configOptions);
        var server = connection.GetServer(connection.GetEndPoints().First());
        await server.FlushAllDatabasesAsync();
        await connection.CloseAsync();
        return CommandResults.Success();
    }
    return CommandResults.Failure("Could not connect to Redis");
});

var api = builder.AddProject<Projects.DartsStats_Api>("dartsapi")
    .WaitFor(keycloak)
    .WithEnvironment("Keycloak__Authority", $"{keycloak.GetEndpoint("http")}/realms/dartsstats")
    .WithReference(redis)
    .WaitFor(redis)
    .WaitFor(sqlServer)
    .WithEnvironment("ConnectionStrings__dartsstats", sqlServer.Resource.ConnectionStringExpression)
    .WithExternalHttpEndpoints()
    
    .WithUrls(context =>
    {
        foreach (var u in context.Urls)
        {
            u.DisplayLocation = UrlDisplayLocation.DetailsOnly;
        }

        // Only show the /scalar URL in the UI
        context.Urls.Add(new ResourceUrlAnnotation()
        {
            Url = "/scalar",
            DisplayText = "OpenAPI Docs",
            Endpoint = context.GetEndpoint("https")
        });
    })
    .PublishAsDockerComposeService((resource, service) =>
    {
        service.Ports =
        [
            "5167:${DARTSAPI_PORT}"
        ];
    });

var devProxy = builder.AddDevProxyExecutable("devproxy")
    //.WithConfigFolder("../data/devproxy")
    //.WithConfigFile("./devproxy.json")
    //.WithProxy()
    .WithConfigFile("../data/devproxy/devproxy.json")
    .WithExplicitStart()
    .WithUrlsToWatch(() => [$"https://en.wikipedia.org/*"]);


devProxy.OnResourceReady(async (resource, evt, cancellationToken) =>
{
    api.WithEnvironment("HTTP_PROXY", "http://localhost:8000")
        .WithEnvironment("HTTPS_PROXY", "http://localhost:8000");

    // Get the ResourceNotificationService to trigger a restart
    var notificationService = evt.Services.GetRequiredService<ResourceNotificationService>();
    var logger = evt.Services.GetRequiredService<ILogger<Program>>();

    logger.LogInformation("Dev Proxy is ready. Restarting API to apply proxy settings...");

    // Find the restart command annotation on the API resource
    var restartCommand = api.Resource.Annotations
        .OfType<ResourceCommandAnnotation>()
        .FirstOrDefault(c => c.Name == KnownResourceCommands.RestartCommand);

    if (restartCommand != null)
    {
        var context = new ExecuteCommandContext()
        {
            ServiceProvider = evt.Services,
            ResourceName = api.Resource.Name,
            CancellationToken = cancellationToken
        };

        await restartCommand.ExecuteCommand(context);
        logger.LogInformation("API resource restarted successfully.");
    }
    else
    {
        logger.LogWarning("Restart command not found on API resource.");
    }
});

builder.AddJavaScriptApp("frontend", "../client", "dev")
    .WithReference(api)
    .WaitFor(api)
    .WithEnvironment("VITE_API_BASE_URL", api.GetEndpoint("http"))
    .WithHttpEndpoint(env: "PORT")
    .WithExternalHttpEndpoints()
    .PublishAsDockerFile(options =>
    {
        options.WithDockerfile("../client");
        options.WithImageTag("latest");
    })
    .PublishAsDockerComposeService((resource, service) =>
    {
        service.Ports =
        [
            "8000:8000"
        ];
    });

builder.Build().Run();
