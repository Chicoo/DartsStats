using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var sqlServer = builder.AddSqlServer("sqlserver")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDbGate()
    .WithDataVolume()
    .AddDatabase("DartsStats", "DartsStats");

var redis = builder.AddRedis("redis")
    .WithRedisInsight()
    .WithRedisCommander()
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume();

var keycloak_username = builder.AddParameter("keycloak-username", "admin");
var keycloak_password = builder.AddParameter("keycloak-password", "admin");

var keycloak = builder.AddKeycloak("keycloak", 8089, keycloak_username, keycloak_password)
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume()
    .WithRealmImport("../../data");

// Add the API project
var api = builder.AddProject<Projects.DartsStats_Api>("dartsStats-api")
    .WaitFor(keycloak)
    .WaitFor(sqlServer)
    .WithEnvironment("Keycloak__Authority", $"{keycloak.GetEndpoint("http")}/realms/dartsstats")
    .WithEnvironment("ConnectionStrings__dartsstats", sqlServer.Resource.ConnectionStringExpression)
    .WithEnvironment("ConnectionStrings__redis", redis.Resource.ConnectionStringExpression);

// Add the React frontend using npm with proper endpoint configuration and API reference
builder.AddJavaScriptApp("dartsStats-frontend", "../client", "dev")
    .WithReference(api)
    .WithEnvironment("VITE_API_BASE_URL", api.GetEndpoint("http"))
    .WithHttpEndpoint(env: "PORT")
    .WithExternalHttpEndpoints();

await builder.Build().RunAsync();
