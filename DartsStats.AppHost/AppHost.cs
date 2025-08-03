var builder = DistributedApplication.CreateBuilder(args);

// Add SQL Server connection string as a parameter that can be configured via Aspire dashboard
// Default example: Server=localhost;Database=DartsStats;Integrated Security=true;TrustServerCertificate=true;
var sqlConnectionString = builder.AddParameter("dartsstats-connectionstring")
    .WithDescription("Connection string for the DartsStats database. Example: Server=localhost;Database=DartsStats;Integrated Security=true;TrustServerCertificate=true;");

// Add the API project
var api = builder.AddProject<Projects.DartsStats_Api>("dartsStats-api")
    .WithEnvironment("ConnectionStrings__dartsstats", sqlConnectionString);

// Add the React frontend using npm with proper endpoint configuration and API reference
builder.AddNpmApp("dartsStats-frontend", "../client", "dev")
    .WithReference(api)
    .WithEnvironment("VITE_API_BASE_URL", api.GetEndpoint("http"))
    .WithHttpEndpoint(env: "PORT")
    .WithExternalHttpEndpoints();

await builder.Build().RunAsync();
