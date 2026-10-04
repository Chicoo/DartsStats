using System.Net.Mime;
using System.Diagnostics;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Protomaps.Host.Extensions;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services),
        writeToProviders: true);

    builder.AddServiceDefaults();

    var protomapsOptions = builder.Configuration.GetSection(ProtomapsOptions.SectionName).Get<ProtomapsOptions>() ?? new ProtomapsOptions();
    var corsOptions = builder.Configuration.GetSection(CorsOptionsConfig.SectionName).Get<CorsOptionsConfig>() ?? new CorsOptionsConfig();

    var publicUrlPath = ProtomapsOptions.NormalizePublicUrlPath(protomapsOptions.PublicUrlPath);
    var mappedPublicUrlPath = ProtomapsOptions.IsLiteralRoute(publicUrlPath) ? publicUrlPath : ProtomapsOptions.DefaultPublicUrlPath;
    var startupValidationError = ValidateConfiguration(protomapsOptions, publicUrlPath, corsOptions);

    builder.Services
        .AddHealthChecks()
        .AddCheck("protomaps-assets", () =>
        {
            if (startupValidationError is not null)
            {
                return HealthCheckResult.Unhealthy(startupValidationError);
            }

            var assetValidationError = ValidateRequiredAssets(protomapsOptions);
            return assetValidationError is null
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy(assetValidationError);
        });

    if (corsOptions.Enabled)
    {
        builder.Services.AddCors(options =>
        {
            options.AddPolicy(CorsOptionsConfig.PolicyName, policy =>
            {
                if (corsOptions.AllowAnyOrigin)
                {
                    policy.AllowAnyOrigin();
                }
                else
                {
                    policy.WithOrigins(corsOptions.AllowedOrigins);
                }

                policy.AllowAnyHeader().AllowAnyMethod();
            });
        });
    }

    var app = builder.Build();
    var logger = app.Logger;

    logger.LogInformation(
        "Protomaps host starting with route '{PublicUrlPath}', file '{FilePath}', content type '{ContentType}', cache control '{CacheControl}', CORS enabled: {CorsEnabled}, allowed origins: {AllowedOriginsCount}.",
        mappedPublicUrlPath,
        protomapsOptions.FilePath,
        protomapsOptions.ContentType,
        protomapsOptions.CacheControl,
        corsOptions.Enabled,
        corsOptions.AllowedOrigins.Length);

    if (startupValidationError is not null)
    {
        logger.LogError(
            "Protomaps host startup validation failed: {ValidationError}. Requests to '{PublicUrlPath}' will return 500 until corrected.",
            startupValidationError,
            mappedPublicUrlPath);
    }

    if (corsOptions.Enabled)
    {
        app.UseCors(CorsOptionsConfig.PolicyName);
    }

    app.Use(async (httpContext, next) =>
    {
        var stopwatch = Stopwatch.StartNew();
        var request = httpContext.Request;
        var rangeHeader = request.Headers.Range.ToString();
        var originHeader = request.Headers.Origin.ToString();

        logger.LogInformation(
            "Incoming request {Method} {Path}{Query} (TraceId: {TraceId}, Origin: {Origin}, Range: {Range}).",
            request.Method,
            request.Path,
            request.QueryString,
            httpContext.TraceIdentifier,
            string.IsNullOrWhiteSpace(originHeader) ? "<none>" : originHeader,
            string.IsNullOrWhiteSpace(rangeHeader) ? "<none>" : rangeHeader);

        await next();

        stopwatch.Stop();

        logger.LogInformation(
            "Completed request {Method} {Path}{Query} with status {StatusCode} in {ElapsedMilliseconds}ms (TraceId: {TraceId}).",
            request.Method,
            request.Path,
            request.QueryString,
            httpContext.Response.StatusCode,
            stopwatch.ElapsedMilliseconds,
            httpContext.TraceIdentifier);
    });

    app.MapHealthChecks("/healthz");

    app.LoadProtomapsEndpoints(mappedPublicUrlPath, protomapsOptions, startupValidationError, logger);

    app.MapGet("/", () =>
    {
        logger.LogInformation("Redirecting root request '/' to '{MappedPublicUrlPath}'.", mappedPublicUrlPath);
        return Results.Redirect(mappedPublicUrlPath, permanent: false);
    });

    app.MapGet("/{**requestedPath}", (HttpContext httpContext, ILogger<Program> logger) =>
    {
        var requestPath = httpContext.Request.Path.Value ?? "/";
        if (!requestPath.Equals(mappedPublicUrlPath, StringComparison.OrdinalIgnoreCase)
            && !requestPath.StartsWith($"{mappedPublicUrlPath.TrimEnd('/')}/", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogWarning(
                "Request path '{RequestPath}' does not match configured Protomaps path '{MappedPublicUrlPath}'. Returning 404.",
                requestPath,
                mappedPublicUrlPath);
            return Results.NotFound();
        }

        if (startupValidationError is not null)
        {
            logger.LogError(
                "Rejecting PMTiles request for '{RequestPath}' due to invalid startup configuration: {ValidationError}.",
                requestPath,
                startupValidationError);
            return Results.Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Invalid Protomaps host configuration",
                detail: startupValidationError);
        }

        return Results.NotFound();
    });

    app.MapDefaultEndpoints();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Protomaps host terminated unexpectedly.");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

static string? ValidateConfiguration(ProtomapsOptions protomapsOptions, string publicUrlPath, CorsOptionsConfig corsOptions)
{
    if (string.IsNullOrWhiteSpace(protomapsOptions.FilePath))
    {
        return $"{ProtomapsOptions.SectionName}:FilePath is required.";
    }

    if (!Path.IsPathRooted(protomapsOptions.FilePath))
    {
        return $"{ProtomapsOptions.SectionName}:FilePath must be an absolute path.";
    }

    if (!Directory.Exists(protomapsOptions.FilePath)
        && string.IsNullOrWhiteSpace(Path.GetExtension(protomapsOptions.FilePath)))
    {
        try
        {
            Directory.CreateDirectory(protomapsOptions.FilePath);
        }
        catch (Exception ex)
        {
            return $"{ProtomapsOptions.SectionName}:FilePath directory could not be created. {ex.Message}";
        }
    }

    if (!Directory.Exists(protomapsOptions.FilePath)
        && !ProtomapsOptions.SupportedFileExtensions.Contains(Path.GetExtension(protomapsOptions.FilePath), StringComparer.OrdinalIgnoreCase))
    {
        return $"{ProtomapsOptions.SectionName}:FilePath must target a directory, .pmtiles file, .maptiles file, or .geojson file.";
    }

    if (!ProtomapsOptions.IsLiteralRoute(publicUrlPath))
    {
        return $"{ProtomapsOptions.SectionName}:PublicUrlPath must be a fixed literal URL path.";
    }

    if (corsOptions.Enabled && !corsOptions.AllowAnyOrigin && corsOptions.AllowedOrigins.Length == 0)
    {
        return $"{CorsOptionsConfig.SectionName}:AllowedOrigins must contain at least one origin when CORS is enabled (or set AllowAnyOrigin to true).";
    }

    return null;
}

static string? ValidateRequiredAssets(ProtomapsOptions protomapsOptions)
{
    if (protomapsOptions.RequiredFiles.Length == 0)
    {
        return null;
    }

    var assetRoot = Directory.Exists(protomapsOptions.FilePath)
        ? Path.GetFullPath(protomapsOptions.FilePath)
        : Path.GetDirectoryName(Path.GetFullPath(protomapsOptions.FilePath));

    if (string.IsNullOrWhiteSpace(assetRoot) || !Directory.Exists(assetRoot))
    {
        return $"Protomaps asset root '{assetRoot}' does not exist.";
    }

    foreach (var requiredFile in protomapsOptions.RequiredFiles)
    {
        if (string.IsNullOrWhiteSpace(requiredFile) || Path.IsPathRooted(requiredFile))
        {
            return "Protomaps required asset paths must be non-empty and relative to the configured asset root.";
        }

        var candidatePath = Path.GetFullPath(Path.Combine(assetRoot, requiredFile));
        var relativeCandidatePath = Path.GetRelativePath(assetRoot, candidatePath);
        if (Path.IsPathRooted(relativeCandidatePath)
            || relativeCandidatePath.Equals("..", StringComparison.Ordinal)
            || relativeCandidatePath.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
        {
            return $"Protomaps required asset path '{requiredFile}' escapes the configured asset root.";
        }

        if (!File.Exists(candidatePath))
        {
            return $"Required Protomaps asset '{requiredFile}' is missing.";
        }
    }

    return null;
}

internal sealed class ProtomapsOptions
{
    public const string SectionName = "Protomaps";
    public const string DefaultPublicUrlPath = "/protomaps";

    public string FilePath { get; init; } = string.Empty;
    public string PublicUrlPath { get; init; } = DefaultPublicUrlPath;
    public string ContentType { get; init; } = MediaTypeNames.Application.Octet;
    public string CacheControl { get; init; } = "public,max-age=3600";

    /// <summary>
    /// Gets the asset paths, relative to <see cref="FilePath" />, that must exist for the service to report healthy.
    /// </summary>
    public string[] RequiredFiles { get; init; } = [];

    public static readonly string[] SupportedFileExtensions = [".pmtiles", ".maptiles", ".geojson"];

    public static string NormalizePublicUrlPath(string rawPath)
    {
        if (string.IsNullOrWhiteSpace(rawPath))
        {
            return DefaultPublicUrlPath;
        }

        var trimmed = rawPath.Trim();
        trimmed = trimmed.StartsWith('/') ? trimmed : $"/{trimmed}";

        return trimmed;
    }

    public static bool IsLiteralRoute(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        return path.StartsWith('/')
            && !path.Contains("..", StringComparison.Ordinal)
            && !path.Contains('{', StringComparison.Ordinal)
            && !path.Contains('}', StringComparison.Ordinal)
            && !path.Contains('*', StringComparison.Ordinal)
            && !path.Contains('?', StringComparison.Ordinal)
            && !path.Contains('#', StringComparison.Ordinal);
    }
}

internal sealed class CorsOptionsConfig
{
    public const string SectionName = "Cors";
    public const string PolicyName = "ProtomapsCors";

    public bool Enabled { get; init; }
    public bool AllowAnyOrigin { get; init; }
    public string[] AllowedOrigins { get; init; } = [];
}

public partial class Program;
