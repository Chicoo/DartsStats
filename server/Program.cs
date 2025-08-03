using Microsoft.EntityFrameworkCore;
using DartsStats.Api.Data;
using DartsStats.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire components.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add Entity Framework with connection string from Aspire
builder.Services.AddDbContext<DartsDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("dartsstats"));
});

// Add our database seeding service
builder.Services.AddScoped<DatabaseSeedService>();

// Add HttpClient for external API calls
builder.Services.AddHttpClient();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            policy
                .AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader();
        }
        else
        {
            policy
                .WithOrigins("http://localhost:3000", "http://localhost:5173")
                .AllowAnyMethod()
                .AllowAnyHeader();
        }
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
// Use CORS first, before other middleware
app.UseCors("AllowFrontend");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.MapDefaultEndpoints();

// Seed data
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<DartsDbContext>();
    await context.Database.EnsureCreatedAsync();
    
    var seedService = scope.ServiceProvider.GetRequiredService<DatabaseSeedService>();
    await seedService.SeedDataAsync();
}

app.Run();
