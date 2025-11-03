using Aspire.Hosting;

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
    .WithRealmImport("../../data")
    .WithExternalHttpEndpoints()
    .PublishAsDockerComposeService((resource, service) =>
    {
        service.Command =
        [
            "start-dev",
            "--import-realm"
        ];
    });

var redis = builder.AddRedis("redis", port: 6380)
    .WithRedisInsight()
    .WithRedisCommander()
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume();

var api = builder.AddProject<Projects.DartsStats_Api>("dartsapi")
    .WaitFor(keycloak)
    .WithEnvironment("Keycloak__Authority", $"{keycloak.GetEndpoint("http")}/realms/dartsstats")
    .WithReference(redis)
    .WaitFor(redis)
    .WaitFor(sqlServer)
    .WithEnvironment("ConnectionStrings__dartsstats", sqlServer.Resource.ConnectionStringExpression)
    .WithExternalHttpEndpoints()
    .PublishAsDockerComposeService((resource, service) =>
    {
        service.Ports =
        [
            "5167:${DARTSAPI_PORT}"
        ];
    });

builder.AddJavaScriptApp("dartsStats-frontend", "../client", "dev")
    .WithReference(api)
    .WaitFor(api)
    .WithEnvironment("VITE_API_BASE_URL", api.GetEndpoint("http"))
    .WithHttpEndpoint(env: "PORT")
    .WithExternalHttpEndpoints()
    .PublishAsDockerFile(options =>
    {
        options.WithDockerfile("../client");
        options.WithImageTag("latest");
        options.WithImageRegistry("acrtopfas.azurecr.io");
    })
    .PublishAsDockerComposeService((resource, service) =>
    {
        service.Ports =
        [
            "8000:8000"
        ];
    });

builder.Build().Run();
