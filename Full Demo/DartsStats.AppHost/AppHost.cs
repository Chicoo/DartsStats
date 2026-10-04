using DartsStats.AppHost;
using DevProxy.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;

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
    .WithOtlpExporter()
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

var devProxy = builder.AddDevProxyContainer("devproxy")
    .WithDeveloperCertificateTrust(true)
    .WithConfigFolder("../data/devproxy")
    .WithConfigFile("./devproxy.json")
    .WithCertFolder("../data/devproxy/cert")
    .WithExplicitStart()
    .WithUrlsToWatch(() => [$"https://en.wikipedia.org/*"]);

var api = builder.AddProject<Projects.DartsStats_Api>("dartsapi")
    .WaitFor(keycloak)
    .WithEnvironment("Keycloak__Authority", $"{keycloak.GetEndpoint("http")}/realms/dartsstats")
    .WithReference(redis)
    .WaitFor(redis)
    .WaitFor(sqlServer)
    .WithDevProxy(devProxy)
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
            Url = "/scalar/v1",
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

var protomapsFilePath = builder.AddParameter("protomaps-file-path",
    Path.GetFullPath(Path.Combine(builder.AppHostDirectory, "../data/protomaps")));

var protomaps = builder.AddProject<Projects.Protomaps_Host>("protomaps", launchProfileName: "https")
    .WithEnvironment("Protomaps__FilePath", protomapsFilePath)
    .WithHttpHealthCheck("/healthz")
    .WithExternalHttpEndpoints()
    .WithUrls(context =>
    {
        context.Urls.Add(new ResourceUrlAnnotation
        {
            Url = "/protomaps/files",
            DisplayText = "Map Files",
            Endpoint = context.GetEndpoint("https")
        });
    });

protomapsFilePath.WithParentRelationship(protomaps);

builder.AddJavaScriptApp("protomaps-demo", "../Services/Protomaps/Protomaps.Demo", "start")
    .WithHttpEndpoint(env: "PORT")
    .WithEnvironment("PROTOMAPS_HTTP", protomaps.GetEndpoint("http"))
    .WithReference(protomaps)
    .WaitFor(protomaps)
    .WithParentRelationship(protomaps)
    .WithExternalHttpEndpoints();

var frontend = builder.AddViteApp("frontend", "../client", "dev")
    .WithReference(protomaps)
    .WithEnvironment("PROTOMAPS_HTTP", protomaps.GetEndpoint("http"))
    .WithReference(api)
    .WaitFor(api)
    .WithEnvironment("VITE_API_BASE_URL", api.GetEndpoint("http"))
    .WithExternalHttpEndpoints()
    .PublishAsDockerComposeService((resource, service) =>
    {
        service.Ports =
        [
            "8000:8000"
        ];
    });

var managementPublicUrl = builder.AddParameter("user-management-public-url", "https://localhost:5178");
var managementClientSecret = builder.AddParameter("user-management-client-secret",
    new GenerateParameterDefault { MinLength = 32, Special = false }, secret: true, persist: true);
var managementAdminSecret = builder.AddParameter("user-management-admin-secret",
    new GenerateParameterDefault { MinLength = 32, Special = false }, secret: true, persist: true);
var demoAdminPassword = builder.AddParameter("user-management-demo-admin-password", secret: true);

var managementProvisioning = builder.AddProject<Projects.UserManagement_Host>("user-management-provisioning", launchProfileName: null)
    .WithArgs("--provision")
    .WithReference(keycloak)
    .WaitFor(keycloak)
    .WithEnvironment("Keycloak__BaseUrl", keycloak.GetEndpoint("http"))
    .WithEnvironment("Keycloak__PublicUrl", managementPublicUrl)
    .WithEnvironment("Keycloak__ClientSecret", managementClientSecret)
    .WithEnvironment("Keycloak__AdminClientSecret", managementAdminSecret)
    .WithEnvironment("Bootstrap__Username", keycloak_username)
    .WithEnvironment("Bootstrap__Password", keycloak_password)
    .WithEnvironment("Bootstrap__SeedAdmin", builder.ExecutionContext.IsRunMode ? "true" : "false")
    .WithEnvironment("Bootstrap__DemoAdminPassword", demoAdminPassword)
    .WithParentRelationship(keycloak)
    .PublishAsDockerFile(container => container.WithArgs("--provision"));

var managementApi = builder.AddProject<Projects.UserManagement_Host>("user-management")
    .WithReference(keycloak)
    .WaitForCompletion(managementProvisioning)
    .WithEnvironment("Keycloak__BaseUrl", keycloak.GetEndpoint("http"))
    .WithEnvironment("Keycloak__PublicUrl", managementPublicUrl)
    .WithEnvironment("Keycloak__ClientSecret", managementClientSecret)
    .WithEnvironment("Keycloak__AdminClientSecret", managementAdminSecret)
    .WithEnvironment("Keycloak__DartsUrl", frontend.GetEndpoint("http"))
    .WithHttpHealthCheck("/health")
    .WithExternalHttpEndpoints()
    .PublishAsDockerFile(container => container.WithAnnotation(new DockerfileBuildAnnotation(
        Path.GetFullPath(Path.Combine(builder.AppHostDirectory, "..")),
        Path.GetFullPath(Path.Combine(builder.AppHostDirectory, "../Services/UserManagement/Dockerfile")), null)));

var managementWeb = builder.AddViteApp("user-management-web", "../Services/UserManagement/UserManagement.Web", "dev")
    .WithEndpoint("http", endpoint => { endpoint.UriScheme = "https"; endpoint.Port = 5178; })
    .WithEnvironment("USER_MANAGEMENT_API", managementApi.GetEndpoint("https"))
    .WithEnvironment("VITE_DARTS_URL", frontend.GetEndpoint("http"))
    .WaitFor(managementApi)
    .WithParentRelationship(managementApi)
    .WithExternalHttpEndpoints()
    .ExcludeFromManifest();

frontend.WithEnvironment("VITE_USER_MANAGEMENT_URL", managementPublicUrl);

builder.Build().Run();
