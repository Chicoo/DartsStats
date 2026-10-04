using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace Protomaps.Host.Tests;

public sealed class ProtomapsEndpointTests
{
    [Fact]
    public void Program_Type_Resolves_To_Host_Assembly()
    {
        Assert.Equal("Protomaps.Host", typeof(Program).Assembly.GetName().Name);
    }

    [Fact]
    public async Task Healthz_Returns_200()
    {
        var filePath = CreateTempPmtilesFile();

        try
        {
            using var env = CreateEnvironmentScope(filePath);
            using var factory = new ProtomapsHostFactory();
            using var client = factory.CreateClient();

            var response = await client.GetAsync("/healthz");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public async Task Healthz_Returns_503_When_A_Required_Asset_Is_Missing()
    {
        var directoryPath = CreateTempDirectory();

        try
        {
            using var env = CreateEnvironmentScope(directoryPath, additionalValues: new Dictionary<string, string?>
            {
                ["Protomaps__RequiredFiles__0"] = "main.pmtiles"
            });
            using var factory = new ProtomapsHostFactory();
            using var client = factory.CreateClient();

            var response = await client.GetAsync("/healthz");

            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        }
        finally
        {
            Directory.Delete(directoryPath, recursive: true);
        }
    }

    [Fact]
    public async Task Healthz_Returns_200_When_All_Required_Assets_Exist()
    {
        var directoryPath = CreateTempDirectory();
        CreateAssetFile(directoryPath, "main.pmtiles", CreatePmtilesBytes(4096));

        try
        {
            using var env = CreateEnvironmentScope(directoryPath, additionalValues: new Dictionary<string, string?>
            {
                ["Protomaps__RequiredFiles__0"] = "main.pmtiles"
            });
            using var factory = new ProtomapsHostFactory();
            using var client = factory.CreateClient();

            var response = await client.GetAsync("/healthz");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
        finally
        {
            Directory.Delete(directoryPath, recursive: true);
        }
    }

    [Fact]
    public async Task Healthz_Returns_503_When_A_Required_Asset_Escapes_The_Asset_Root()
    {
        var directoryPath = CreateTempDirectory();

        try
        {
            using var env = CreateEnvironmentScope(directoryPath, additionalValues: new Dictionary<string, string?>
            {
                ["Protomaps__RequiredFiles__0"] = Path.Combine("..", "outside.pmtiles")
            });
            using var factory = new ProtomapsHostFactory();
            using var client = factory.CreateClient();

            var response = await client.GetAsync("/healthz");

            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        }
        finally
        {
            Directory.Delete(directoryPath, recursive: true);
        }
    }

    [Fact]
    public async Task Root_Redirects_To_Default_Protomaps_Path()
    {
        var filePath = CreateTempPmtilesFile();

        try
        {
            using var env = CreateEnvironmentScope(filePath);
            using var factory = new ProtomapsHostFactory();
            using var client = factory.CreateClient(new()
            {
                AllowAutoRedirect = false
            });

            var response = await client.GetAsync("/");

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Equal("/protomaps/main.pmtiles", response.Headers.Location?.OriginalString);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public async Task Default_Public_Route_Exposes_The_File_Overview()
    {
        var filePath = CreateTempPmtilesFile();

        try
        {
            using var env = CreateEnvironmentScope(filePath, publicUrlPath: null);
            using var factory = new ProtomapsHostFactory();
            using var client = factory.CreateClient(new()
            {
                AllowAutoRedirect = false
            });

            var rootResponse = await client.GetAsync("/");
            var filesResponse = await client.GetAsync("/protomaps/files");

            Assert.Equal(HttpStatusCode.Redirect, rootResponse.StatusCode);
            Assert.Equal("/protomaps", rootResponse.Headers.Location?.OriginalString);
            Assert.Equal(HttpStatusCode.OK, filesResponse.StatusCode);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public async Task Full_File_Request_Returns_200_With_Configured_Headers()
    {
        var filePath = CreateTempPmtilesFile();

        try
        {
            using var env = CreateEnvironmentScope(filePath, additionalValues: new Dictionary<string, string?>
            {
                ["Protomaps__ContentType"] = "application/vnd.pmtiles",
                ["Protomaps__CacheControl"] = "public,max-age=120"
            });
            using var factory = new ProtomapsHostFactory();
            using var client = factory.CreateClient();

            var response = await client.GetAsync("/protomaps/main.pmtiles");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("application/vnd.pmtiles", response.Content.Headers.ContentType?.MediaType);
            var cacheControl = response.Headers.CacheControl?.ToString();
            Assert.NotNull(cacheControl);
            Assert.Contains("public", cacheControl!, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("max-age=120", cacheControl!, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public async Task Directory_FilePath_Serves_Any_Pmtiles_File_By_Name()
    {
        var directoryPath = CreateTempDirectory();
        var filePath = Path.Combine(directoryPath, "world.pmtiles");
        File.WriteAllBytes(filePath, CreatePmtilesBytes(4096));

        try
        {
            using var env = CreateEnvironmentScope(directoryPath, additionalValues: new Dictionary<string, string?>
            {
                ["Protomaps__PublicUrlPath"] = "/protomaps"
            });
            using var factory = new ProtomapsHostFactory();
            using var client = factory.CreateClient();

            var response = await client.GetAsync("/protomaps/world.pmtiles");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("application/octet-stream", response.Content.Headers.ContentType?.MediaType);
        }
        finally
        {
            Directory.Delete(directoryPath, recursive: true);
        }
    }

    [Fact]
    public async Task Missing_Directory_FilePath_Is_Created_And_Files_Endpoint_Returns_Empty_Overview()
    {
        var directoryPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Assert.False(Directory.Exists(directoryPath));

        try
        {
            using var env = CreateEnvironmentScope(directoryPath, additionalValues: new Dictionary<string, string?>
            {
                ["Protomaps__PublicUrlPath"] = "/protomaps"
            });
            using var factory = new ProtomapsHostFactory();
            using var client = factory.CreateClient();

            var overview = await client.GetFromJsonAsync<ProtomapsFileOverview>("/protomaps/files", JsonSerializerOptions.Web);

            Assert.True(Directory.Exists(directoryPath));
            Assert.NotNull(overview);
            Assert.Empty(overview!.Files);
            Assert.Empty(overview.Glyphs);
            Assert.Empty(overview.SpriteVersions);
        }
        finally
        {
            if (Directory.Exists(directoryPath))
            {
                Directory.Delete(directoryPath, recursive: true);
            }
        }
    }

    [Fact]
    public async Task Directory_FilePath_Serves_Any_Geojson_File_By_Name()
    {
        var directoryPath = CreateTempDirectory();
        var filePath = Path.Combine(directoryPath, "areas.geojson");
        await File.WriteAllTextAsync(filePath, "{\"type\":\"FeatureCollection\",\"features\":[]}");

        try
        {
            using var env = CreateEnvironmentScope(directoryPath, additionalValues: new Dictionary<string, string?>
            {
                ["Protomaps__PublicUrlPath"] = "/protomaps"
            });
            using var factory = new ProtomapsHostFactory();
            using var client = factory.CreateClient();

            var response = await client.GetAsync("/protomaps/areas.geojson");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("application/geo+json", response.Content.Headers.ContentType?.MediaType);
        }
        finally
        {
            Directory.Delete(directoryPath, recursive: true);
        }
    }

    [Fact]
    public async Task Directory_FilePath_Does_Not_Serve_Unsupported_Extensions()
    {
        var directoryPath = CreateTempDirectory();
        await File.WriteAllTextAsync(Path.Combine(directoryPath, "notes.txt"), "not a map");

        try
        {
            using var env = CreateEnvironmentScope(directoryPath, additionalValues: new Dictionary<string, string?>
            {
                ["Protomaps__PublicUrlPath"] = "/protomaps"
            });
            using var factory = new ProtomapsHostFactory();
            using var client = factory.CreateClient();

            var response = await client.GetAsync("/protomaps/notes.txt");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
        finally
        {
            Directory.Delete(directoryPath, recursive: true);
        }
    }

    [Fact]
    public async Task Directory_FilePath_Files_Endpoint_Returns_Supported_File_Overview()
    {
        var directoryPath = CreateTempDirectory();
        File.WriteAllBytes(Path.Combine(directoryPath, "world.pmtiles"), CreatePmtilesBytes(4096));
        await File.WriteAllTextAsync(Path.Combine(directoryPath, "areas.geojson"), "{\"type\":\"FeatureCollection\",\"features\":[]}");
        await File.WriteAllTextAsync(Path.Combine(directoryPath, "notes.txt"), "not a map");
        CreateAssetFile(directoryPath, Path.Combine("fonts", "Noto Sans Regular", "0-255.pbf"), [1, 2, 3]);
        CreateAssetFile(directoryPath, Path.Combine("fonts", "Noto Sans Regular", "256-511.pbf"), [4, 5, 6]);
        CreateAssetFile(directoryPath, Path.Combine("sprites", "v4", "light.json"), "{}"u8.ToArray());

        try
        {
            using var env = CreateEnvironmentScope(directoryPath, additionalValues: new Dictionary<string, string?>
            {
                ["Protomaps__PublicUrlPath"] = "/protomaps"
            });
            using var factory = new ProtomapsHostFactory();
            using var client = factory.CreateClient();

            var overview = await client.GetFromJsonAsync<ProtomapsFileOverview>("/protomaps/files", JsonSerializerOptions.Web);

            Assert.NotNull(overview);
            Assert.Collection(
                overview!.Files,
                file =>
                {
                    Assert.Equal("areas.geojson", file.Name);
                    Assert.Equal("geojson", file.Type);
                    Assert.Equal("/protomaps/areas.geojson", file.Url);
                    Assert.Equal("application/geo+json", file.ContentType);
                    Assert.NotNull(file.SizeBytes);
                    Assert.NotNull(file.LastModifiedUtc);
                },
                file =>
                {
                    Assert.Equal("world.pmtiles", file.Name);
                    Assert.Equal("pmtiles", file.Type);
                    Assert.Equal("/protomaps/world.pmtiles", file.Url);
                    Assert.Equal("application/octet-stream", file.ContentType);
                    Assert.Equal(4096, file.SizeBytes);
                    Assert.NotNull(file.LastModifiedUtc);
                });
            var glyph = Assert.Single(overview.Glyphs);
            Assert.Equal("Noto Sans Regular", glyph.Fontstack);
            Assert.Equal(["0-255.pbf", "256-511.pbf"], glyph.Ranges);
            Assert.Equal("/protomaps/fonts/Noto%20Sans%20Regular/{range}.pbf", glyph.UrlTemplate);
            Assert.Equal(["v4"], overview.SpriteVersions);
        }
        finally
        {
            Directory.Delete(directoryPath, recursive: true);
        }
    }

    [Fact]
    public async Task Single_FilePath_Files_Endpoint_Returns_Configured_File_Overview()
    {
        var directoryPath = CreateTempDirectory();
        var filePath = Path.Combine(directoryPath, "main.pmtiles");
        File.WriteAllBytes(filePath, CreatePmtilesBytes(4096));
        CreateAssetFile(directoryPath, Path.Combine("fonts", "Noto Sans Regular", "0-255.pbf"), [1, 2, 3]);
        CreateAssetFile(directoryPath, Path.Combine("sprites", "v4", "light.json"), "{}"u8.ToArray());

        try
        {
            using var env = CreateEnvironmentScope(filePath, additionalValues: new Dictionary<string, string?>
            {
                ["Protomaps__ContentType"] = "application/vnd.pmtiles"
            });
            using var factory = new ProtomapsHostFactory();
            using var client = factory.CreateClient();

            var overview = await client.GetFromJsonAsync<ProtomapsFileOverview>("/protomaps/main.pmtiles/files", JsonSerializerOptions.Web);

            Assert.NotNull(overview);
            var file = Assert.Single(overview!.Files);
            Assert.Equal(Path.GetFileName(filePath), file.Name);
            Assert.Equal("pmtiles", file.Type);
            Assert.Equal("/protomaps/main.pmtiles", file.Url);
            Assert.Equal("application/vnd.pmtiles", file.ContentType);
            Assert.NotNull(file.SizeBytes);
            Assert.NotNull(file.LastModifiedUtc);
            var glyph = Assert.Single(overview.Glyphs);
            Assert.Equal("Noto Sans Regular", glyph.Fontstack);
            Assert.Equal(["0-255.pbf"], glyph.Ranges);
            Assert.Equal("/protomaps/fonts/Noto%20Sans%20Regular/{range}.pbf", glyph.UrlTemplate);
            Assert.Equal(["v4"], overview.SpriteVersions);
        }
        finally
        {
            Directory.Delete(directoryPath, recursive: true);
        }
    }

    [Fact]
    public async Task Directory_FilePath_Serves_Glyph_Asset_By_Fontstack_And_Range()
    {
        var directoryPath = CreateTempDirectory();
        CreateAssetFile(directoryPath, Path.Combine("fonts", "Noto Sans Regular", "0-255.pbf"), [1, 2, 3]);

        try
        {
            using var env = CreateEnvironmentScope(directoryPath, additionalValues: new Dictionary<string, string?>
            {
                ["Protomaps__PublicUrlPath"] = "/protomaps"
            });
            using var factory = new ProtomapsHostFactory();
            using var client = factory.CreateClient();

            var response = await client.GetAsync("/protomaps/fonts/Noto%20Sans%20Regular/0-255.pbf");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("application/x-protobuf", response.Content.Headers.ContentType?.MediaType);
        }
        finally
        {
            Directory.Delete(directoryPath, recursive: true);
        }
    }

    [Fact]
    public async Task Single_FilePath_Serves_Glyph_Asset_From_Pmtiles_Sibling_Folder()
    {
        var directoryPath = CreateTempDirectory();
        var filePath = Path.Combine(directoryPath, "main.pmtiles");
        File.WriteAllBytes(filePath, CreatePmtilesBytes(4096));
        CreateAssetFile(directoryPath, Path.Combine("fonts", "Noto Sans Regular", "0-255.pbf"), [1, 2, 3]);

        try
        {
            using var env = CreateEnvironmentScope(filePath);
            using var factory = new ProtomapsHostFactory();
            using var client = factory.CreateClient();

            var response = await client.GetAsync("/protomaps/fonts/Noto%20Sans%20Regular/0-255.pbf");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("application/x-protobuf", response.Content.Headers.ContentType?.MediaType);
        }
        finally
        {
            Directory.Delete(directoryPath, recursive: true);
        }
    }

    [Theory]
    [InlineData("/protomaps/sprites/v4/light.json", "application/json")]
    [InlineData("/protomaps/sprites/v4/light.png", "image/png")]
    [InlineData("/protomaps/sprites/v4/light@2x.json", "application/json")]
    [InlineData("/protomaps/sprites/v4/light@2x.png", "image/png")]
    public async Task Directory_FilePath_Serves_Sprite_Assets(string requestPath, string expectedContentType)
    {
        var directoryPath = CreateTempDirectory();
        CreateAssetFile(directoryPath, Path.Combine("sprites", "v4", "light.json"), "{}"u8.ToArray());
        CreateAssetFile(directoryPath, Path.Combine("sprites", "v4", "light.png"), [137, 80, 78, 71]);
        CreateAssetFile(directoryPath, Path.Combine("sprites", "v4", "light@2x.json"), "{}"u8.ToArray());
        CreateAssetFile(directoryPath, Path.Combine("sprites", "v4", "light@2x.png"), [137, 80, 78, 71]);

        try
        {
            using var env = CreateEnvironmentScope(directoryPath, additionalValues: new Dictionary<string, string?>
            {
                ["Protomaps__PublicUrlPath"] = "/protomaps"
            });
            using var factory = new ProtomapsHostFactory();
            using var client = factory.CreateClient();

            var response = await client.GetAsync(requestPath);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(expectedContentType, response.Content.Headers.ContentType?.MediaType);
        }
        finally
        {
            Directory.Delete(directoryPath, recursive: true);
        }
    }

    [Fact]
    public async Task Missing_Asset_Returns_404()
    {
        var directoryPath = CreateTempDirectory();
        File.WriteAllBytes(Path.Combine(directoryPath, "main.pmtiles"), CreatePmtilesBytes(4096));

        try
        {
            using var env = CreateEnvironmentScope(directoryPath, additionalValues: new Dictionary<string, string?>
            {
                ["Protomaps__PublicUrlPath"] = "/protomaps"
            });
            using var factory = new ProtomapsHostFactory();
            using var client = factory.CreateClient();

            var response = await client.GetAsync("/protomaps/sprites/v4/light.json");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
        finally
        {
            Directory.Delete(directoryPath, recursive: true);
        }
    }

    [Fact]
    public async Task Unsupported_Sprite_Extension_Returns_404()
    {
        var directoryPath = CreateTempDirectory();
        CreateAssetFile(directoryPath, Path.Combine("sprites", "v4", "light.txt"), "bad"u8.ToArray());

        try
        {
            using var env = CreateEnvironmentScope(directoryPath, additionalValues: new Dictionary<string, string?>
            {
                ["Protomaps__PublicUrlPath"] = "/protomaps"
            });
            using var factory = new ProtomapsHostFactory();
            using var client = factory.CreateClient();

            var response = await client.GetAsync("/protomaps/sprites/v4/light.txt");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
        finally
        {
            Directory.Delete(directoryPath, recursive: true);
        }
    }

    [Theory]
    [InlineData("/protomaps/fonts/%2e%2e/0-255.pbf")]
    [InlineData("/protomaps/fonts/Noto%20Sans%20Regular/%2e%2e%2fsecrets.pbf")]
    [InlineData("/protomaps/sprites/%2e%2e/light.json")]
    [InlineData("/protomaps/sprites/v4/%2e%2e%2fsecret.png")]
    public async Task Asset_Path_Traversal_Attempt_Is_Not_Routable(string requestPath)
    {
        var directoryPath = CreateTempDirectory();
        File.WriteAllBytes(Path.Combine(directoryPath, "main.pmtiles"), CreatePmtilesBytes(4096));

        try
        {
            using var env = CreateEnvironmentScope(directoryPath, additionalValues: new Dictionary<string, string?>
            {
                ["Protomaps__PublicUrlPath"] = "/protomaps"
            });
            using var factory = new ProtomapsHostFactory();
            using var client = factory.CreateClient();

            var response = await client.GetAsync(requestPath);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
        finally
        {
            Directory.Delete(directoryPath, recursive: true);
        }
    }

    [Fact]
    public async Task Asset_Responses_Use_Configured_Cache_Control()
    {
        var directoryPath = CreateTempDirectory();
        CreateAssetFile(directoryPath, Path.Combine("sprites", "v4", "light.json"), "{}"u8.ToArray());

        try
        {
            using var env = CreateEnvironmentScope(directoryPath, additionalValues: new Dictionary<string, string?>
            {
                ["Protomaps__PublicUrlPath"] = "/protomaps",
                ["Protomaps__CacheControl"] = "public,max-age=120"
            });
            using var factory = new ProtomapsHostFactory();
            using var client = factory.CreateClient();

            var response = await client.GetAsync("/protomaps/sprites/v4/light.json");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var cacheControl = response.Headers.CacheControl?.ToString();
            Assert.NotNull(cacheControl);
            Assert.Contains("public", cacheControl!, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("max-age=120", cacheControl!, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(directoryPath, recursive: true);
        }
    }

    [Fact]
    public async Task Range_Request_Returns_206_And_ContentRange()
    {
        var filePath = CreateTempPmtilesFile();

        try
        {
            using var env = CreateEnvironmentScope(filePath);
            using var factory = new ProtomapsHostFactory();
            using var client = factory.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Get, "/protomaps/main.pmtiles");
            request.Headers.Range = new RangeHeaderValue(0, 9);

            var response = await client.SendAsync(request);

            Assert.Equal(HttpStatusCode.PartialContent, response.StatusCode);
            Assert.NotNull(response.Content.Headers.ContentRange);
            Assert.Equal(0, response.Content.Headers.ContentRange!.From);
            Assert.Equal(9, response.Content.Headers.ContentRange!.To);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public async Task Missing_File_Returns_404()
    {
        var missingPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.pmtiles");

        using var env = CreateEnvironmentScope(missingPath);
        using var factory = new ProtomapsHostFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/protomaps/main.pmtiles");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Invalid_FilePath_Config_Returns_500()
    {
        using var env = CreateEnvironmentScope("relative/path/main.pmtiles");
        using var factory = new ProtomapsHostFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/protomaps/main.pmtiles");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [Fact]
    public async Task Cors_Allowed_Origin_Returns_Header_And_Denied_Does_Not()
    {
        var filePath = CreateTempPmtilesFile();

        try
        {
            using var env = CreateEnvironmentScope(filePath, additionalValues: new Dictionary<string, string?>
            {
                ["Cors__Enabled"] = "true",
                ["Cors__AllowedOrigins__0"] = "https://frontend.example"
            });
            using var factory = new ProtomapsHostFactory();
            using var client = factory.CreateClient();

            var allowedRequest = new HttpRequestMessage(HttpMethod.Get, "/protomaps/main.pmtiles");
            allowedRequest.Headers.Add("Origin", "https://frontend.example");
            var allowedResponse = await client.SendAsync(allowedRequest);

            Assert.Equal(HttpStatusCode.OK, allowedResponse.StatusCode);
            Assert.True(allowedResponse.Headers.TryGetValues("Access-Control-Allow-Origin", out var allowedHeader));
            Assert.Equal("https://frontend.example", allowedHeader!.Single());

            var deniedRequest = new HttpRequestMessage(HttpMethod.Get, "/protomaps/main.pmtiles");
            deniedRequest.Headers.Add("Origin", "https://denied.example");
            var deniedResponse = await client.SendAsync(deniedRequest);

            Assert.Equal(HttpStatusCode.OK, deniedResponse.StatusCode);
            Assert.False(deniedResponse.Headers.TryGetValues("Access-Control-Allow-Origin", out _));
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public async Task Path_Traversal_Attempt_Is_Not_Routable()
    {
        var filePath = CreateTempPmtilesFile();

        try
        {
            using var env = CreateEnvironmentScope(filePath);
            using var factory = new ProtomapsHostFactory();
            using var client = factory.CreateClient();

            var response = await client.GetAsync("/protomaps/%2e%2e/secret.pmtiles");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    private static EnvironmentVariableScope CreateEnvironmentScope(
        string filePath,
        Dictionary<string, string?>? additionalValues = null,
        string? publicUrlPath = "/protomaps/main.pmtiles")
    {
        var values = new Dictionary<string, string?>
        {
            ["Protomaps__FilePath"] = filePath,
            ["Protomaps__ContentType"] = "application/octet-stream",
            ["Protomaps__CacheControl"] = "public,max-age=3600",
            ["Cors__Enabled"] = "false",
            ["Cors__AllowAnyOrigin"] = "false",
            ["Cors__AllowedOrigins__0"] = null,
            ["Cors__AllowedOrigins__1"] = null,
            ["Protomaps__RequiredFiles__0"] = null,
            ["Protomaps__RequiredFiles__1"] = null
        };

        if (publicUrlPath is not null)
        {
            values["Protomaps__PublicUrlPath"] = publicUrlPath;
        }

        if (additionalValues is not null)
        {
            foreach (var value in additionalValues)
            {
                values[value.Key] = value.Value;
            }
        }

        return new EnvironmentVariableScope(values);
    }

    private static string CreateTempPmtilesFile()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.pmtiles");
        File.WriteAllBytes(path, CreatePmtilesBytes(4096));
        return path;
    }

    private static byte[] CreatePmtilesBytes(int length)
    {
        var data = Enumerable.Range(0, length).Select(i => (byte)(i % 256)).ToArray();
        "PMTiles"u8.CopyTo(data);
        return data;
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static void CreateAssetFile(string directoryPath, string relativePath, byte[] content)
    {
        var filePath = Path.Combine(directoryPath, relativePath);
        var parentDirectory = Path.GetDirectoryName(filePath)!;
        Directory.CreateDirectory(parentDirectory);
        File.WriteAllBytes(filePath, content);
    }

    private sealed class EnvironmentVariableScope : IDisposable
    {
        private readonly Dictionary<string, string?> _originalValues = new();

        public EnvironmentVariableScope(IReadOnlyDictionary<string, string?> values)
        {
            foreach (var value in values)
            {
                _originalValues[value.Key] = Environment.GetEnvironmentVariable(value.Key);
                Environment.SetEnvironmentVariable(value.Key, value.Value);
            }
        }

        public void Dispose()
        {
            foreach (var originalValue in _originalValues)
            {
                Environment.SetEnvironmentVariable(originalValue.Key, originalValue.Value);
            }
        }
    }

    private sealed record ProtomapsFileOverview(
        IReadOnlyCollection<ProtomapsFileEntry> Files,
        IReadOnlyCollection<ProtomapsGlyphEntry> Glyphs,
        IReadOnlyCollection<string> SpriteVersions);

    private sealed record ProtomapsFileEntry(string Name, string Type, string Url, string ContentType, long? SizeBytes, DateTimeOffset? LastModifiedUtc);

    private sealed record ProtomapsGlyphEntry(string Fontstack, IReadOnlyCollection<string> Ranges, string UrlTemplate);
}
