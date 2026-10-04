namespace Protomaps.Host.Extensions
{
    using Microsoft.AspNetCore.Builder;
    using Microsoft.Extensions.Logging;

    public static class ProtomapEndpointsExtension
    {
        private static readonly Dictionary<string, string> SupportedContentTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            [".maptiles"] = "application/octet-stream",
            [".pmtiles"] = "application/octet-stream",
            [".geojson"] = "application/geo+json"
        };

        private static readonly Dictionary<string, string> SupportedSpriteContentTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            [".json"] = "application/json",
            [".png"] = "image/png"
        };

        private const string GlyphContentType = "application/x-protobuf";

        internal static void LoadProtomapsEndpoints(this WebApplication app, string mappedPublicUrlPath, ProtomapsOptions protomapsOptions, string? startupValidationError, ILogger logger)
        {
            if (startupValidationError != null)
            {
                logger.LogCritical("Protomaps service failed to start due to configuration error: {StartupValidationError}", startupValidationError);
                return;
            }

            if (!ProtomapsOptions.IsLiteralRoute(mappedPublicUrlPath))
            {
                logger.LogCritical("Protomaps service failed to start because the configured PublicUrlPath '{MappedPublicUrlPath}' is not a valid literal route. Please ensure it starts with '/' and does not contain route parameters or special characters.", mappedPublicUrlPath);
                return;
            }

            logger.LogInformation("Loading Protomaps endpoints at '{MappedPublicUrlPath}' serving maps from '{FilePath}'.", mappedPublicUrlPath, protomapsOptions.FilePath);

            var routeFileName = Path.GetFileName(mappedPublicUrlPath.TrimEnd('/'));
            var routeTargetsFile = SupportedContentTypes.ContainsKey(Path.GetExtension(routeFileName));

            if (routeTargetsFile && !Directory.Exists(protomapsOptions.FilePath))
            {
                app.MapGet(mappedPublicUrlPath, (HttpContext httpContext) => ServeFile(httpContext, routeFileName, mappedPublicUrlPath, protomapsOptions, logger));
            }
            else
            {
                app.MapGet(mappedPublicUrlPath, () => Results.Redirect(AppendPath(mappedPublicUrlPath, "main.pmtiles"), permanent: false));
            }

            var assetRouteBase = GetAssetRouteBase(mappedPublicUrlPath);

            app.MapGet($"{assetRouteBase}/fonts/{{fontstack}}/{{rangeFile}}", (HttpContext httpContext, string fontstack, string rangeFile) => ServeGlyph(httpContext, fontstack, rangeFile, protomapsOptions, logger));
            app.MapGet($"{assetRouteBase}/sprites/{{version}}/{{spriteFile}}", (HttpContext httpContext, string version, string spriteFile) => ServeSprite(httpContext, version, spriteFile, protomapsOptions, logger));
            app.MapGet($"{mappedPublicUrlPath.TrimEnd('/')}/files", (HttpContext httpContext) => ListFiles(httpContext, mappedPublicUrlPath, protomapsOptions, logger));
            app.MapGet($"{mappedPublicUrlPath.TrimEnd('/')}/{{fileName}}", (HttpContext httpContext, string fileName) => ServeFile(httpContext, fileName, mappedPublicUrlPath, protomapsOptions, logger));
        }

        private static IResult ListFiles(HttpContext httpContext, string mappedPublicUrlPath, ProtomapsOptions protomapsOptions, ILogger logger)
        {
            var requestPath = httpContext.Request.Path.Value ?? "/";

            try
            {
                var files = GetAvailableFiles(mappedPublicUrlPath, protomapsOptions)
                    .OrderBy(file => file.Name, StringComparer.OrdinalIgnoreCase)
                    .ToArray();
                var glyphs = GetAvailableGlyphs(protomapsOptions, GetAssetRouteBase(mappedPublicUrlPath))
                    .OrderBy(glyph => glyph.Fontstack, StringComparer.OrdinalIgnoreCase)
                    .ToArray();
                var spriteVersions = GetAvailableSpriteVersions(protomapsOptions)
                    .OrderBy(version => version, StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                logger.LogDebug(
                    "Returning overview of {FileCount} Protomaps files, {GlyphCount} glyph fontstacks, and {SpriteVersionCount} sprite versions for '{RequestPath}'.",
                    files.Length,
                    glyphs.Length,
                    spriteVersions.Length,
                    requestPath);

                return Results.Ok(new ProtomapsFileOverview(files, glyphs, spriteVersions));
            }
            catch (UnauthorizedAccessException exception)
            {
                logger.LogError(exception, "Read access denied while listing Protomaps files from path '{Path}'.", protomapsOptions.FilePath);

                return Results.Problem(
                    statusCode: StatusCodes.Status403Forbidden,
                    title: "Access to Protomaps file overview denied",
                    detail: "The service account is not allowed to list the configured Protomaps files.");
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to list Protomaps files from path '{Path}'.", protomapsOptions.FilePath);

                return Results.Problem(
                    statusCode: StatusCodes.Status500InternalServerError,
                    title: "Failed to list Protomaps files",
                    detail: "The server failed while listing the configured Protomaps files.");
            }
        }

        private static IResult ServeFile(HttpContext httpContext, string fileName, string mappedPublicUrlPath, ProtomapsOptions protomapsOptions, ILogger logger)
        {
            var requestPath = httpContext.Request.Path.Value ?? "/";
            var filePath = ResolveMappedFilePath(protomapsOptions.FilePath, fileName, mappedPublicUrlPath);

            if (filePath is null)
            {
                logger.LogWarning("Request path '{RequestPath}' is not a supported Protomaps file request.", requestPath);
                return Results.NotFound();
            }

            if (!File.Exists(filePath))
            {
                logger.LogWarning("Requested Protomaps file '{FilePath}' does not exist.", filePath);
                return Results.Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Protomaps file not found",
                    detail: $"The requested file '{fileName}' does not exist.");
            }

            try
            {
                if (IsInvalidPmtilesArchive(filePath))
                {
                    logger.LogWarning("Requested file '{FilePath}' has a .pmtiles extension but does not contain a PMTiles archive header.", filePath);
                    return Results.Problem(
                        statusCode: StatusCodes.Status415UnsupportedMediaType,
                        title: "Invalid PMTiles archive",
                        detail: $"The requested file '{fileName}' has a .pmtiles extension but is not a PMTiles archive.");
                }

                var fileInfo = new FileInfo(filePath);
                var requestedRange = httpContext.Request.Headers.Range.ToString();
                var contentType = GetContentType(filePath, protomapsOptions);

                logger.LogDebug(
                    "Serving Protomaps file '{FilePath}' ({FileLengthBytes} bytes) for '{RequestPath}' with range header '{RangeHeader}'.",
                    filePath,
                    fileInfo.Length,
                    requestPath,
                    string.IsNullOrWhiteSpace(requestedRange) ? "<none>" : requestedRange);

                if (!string.IsNullOrWhiteSpace(protomapsOptions.CacheControl))
                {
                    httpContext.Response.Headers.CacheControl = protomapsOptions.CacheControl;
                }

                return Results.File(
                    path: filePath,
                    contentType: contentType,
                    enableRangeProcessing: true,
                    lastModified: File.GetLastWriteTimeUtc(filePath));
            }
            catch (UnauthorizedAccessException exception)
            {
                logger.LogError(exception, "Read access denied for Protomaps file path '{Path}'.", filePath);

                return Results.Problem(
                    statusCode: StatusCodes.Status403Forbidden,
                    title: "Access to Protomaps file denied",
                    detail: "The service account is not allowed to read the requested Protomaps file.");
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to serve Protomaps file from path '{Path}'.", filePath);

                return Results.Problem(
                    statusCode: StatusCodes.Status500InternalServerError,
                    title: "Failed to serve Protomaps file",
                    detail: "The server failed while reading the requested Protomaps file.");
            }
        }

        private static IResult ServeGlyph(HttpContext httpContext, string fontstack, string rangeFile, ProtomapsOptions protomapsOptions, ILogger logger)
        {
            var requestPath = httpContext.Request.Path.Value ?? "/";

            if (!IsSafePathSegment(fontstack) || !IsSafeFileName(rangeFile) || !string.Equals(Path.GetExtension(rangeFile), ".pbf", StringComparison.OrdinalIgnoreCase))
            {
                logger.LogWarning("Request path '{RequestPath}' is not a supported Protomaps glyph request.", requestPath);
                return Results.NotFound();
            }

            var filePath = ResolveAssetFilePath(protomapsOptions.FilePath, "fonts", fontstack, rangeFile);

            return ServeAssetFile(httpContext, filePath, GlyphContentType, "glyph", protomapsOptions, logger);
        }

        private static IResult ServeSprite(HttpContext httpContext, string version, string spriteFile, ProtomapsOptions protomapsOptions, ILogger logger)
        {
            var requestPath = httpContext.Request.Path.Value ?? "/";
            var extension = Path.GetExtension(spriteFile);

            if (!IsSafePathSegment(version) || !IsSafeFileName(spriteFile) || !SupportedSpriteContentTypes.TryGetValue(extension, out var contentType))
            {
                logger.LogWarning("Request path '{RequestPath}' is not a supported Protomaps sprite request.", requestPath);
                return Results.NotFound();
            }

            var filePath = ResolveAssetFilePath(protomapsOptions.FilePath, "sprites", version, spriteFile);

            return ServeAssetFile(httpContext, filePath, contentType, "sprite", protomapsOptions, logger);
        }

        private static IResult ServeAssetFile(HttpContext httpContext, string? filePath, string contentType, string assetType, ProtomapsOptions protomapsOptions, ILogger logger)
        {
            var requestPath = httpContext.Request.Path.Value ?? "/";

            if (filePath is null)
            {
                logger.LogWarning("Request path '{RequestPath}' is not a supported Protomaps {AssetType} asset request.", requestPath, assetType);
                return Results.NotFound();
            }

            if (!File.Exists(filePath))
            {
                logger.LogWarning("Requested Protomaps {AssetType} asset '{FilePath}' does not exist.", assetType, filePath);
                return Results.NotFound();
            }

            try
            {
                var fileInfo = new FileInfo(filePath);

                logger.LogDebug(
                    "Serving Protomaps {AssetType} asset '{FilePath}' ({FileLengthBytes} bytes) for '{RequestPath}'.",
                    assetType,
                    filePath,
                    fileInfo.Length,
                    requestPath);

                if (!string.IsNullOrWhiteSpace(protomapsOptions.CacheControl))
                {
                    httpContext.Response.Headers.CacheControl = protomapsOptions.CacheControl;
                }

                return Results.File(
                    path: filePath,
                    contentType: contentType,
                    lastModified: File.GetLastWriteTimeUtc(filePath));
            }
            catch (UnauthorizedAccessException exception)
            {
                logger.LogError(exception, "Read access denied for Protomaps {AssetType} asset path '{Path}'.", assetType, filePath);

                return Results.Problem(
                    statusCode: StatusCodes.Status403Forbidden,
                    title: "Access to Protomaps asset denied",
                    detail: "The service account is not allowed to read the requested Protomaps asset.");
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to serve Protomaps {AssetType} asset from path '{Path}'.", assetType, filePath);

                return Results.Problem(
                    statusCode: StatusCodes.Status500InternalServerError,
                    title: "Failed to serve Protomaps asset",
                    detail: "The server failed while reading the requested Protomaps asset.");
            }
        }

        private static string? ResolveMappedFilePath(string configuredPath, string fileName, string mappedPublicUrlPath)
        {
            if (!IsSafeFileName(fileName) || !SupportedContentTypes.ContainsKey(Path.GetExtension(fileName)))
            {
                return null;
            }

            if (Directory.Exists(configuredPath))
            {
                var rootPath = Path.GetFullPath(configuredPath);
                var requestedPath = Path.GetFullPath(Path.Combine(rootPath, fileName));

                return requestedPath.StartsWith(rootPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                    ? requestedPath
                    : null;
            }

            var configuredFileName = Path.GetFileName(configuredPath);
            var configuredRouteFileName = Path.GetFileName(mappedPublicUrlPath.TrimEnd('/'));

            return string.Equals(fileName, configuredFileName, StringComparison.OrdinalIgnoreCase)
                || string.Equals(fileName, configuredRouteFileName, StringComparison.OrdinalIgnoreCase)
                    ? configuredPath
                    : null;
        }

        private static string? ResolveAssetFilePath(string configuredPath, params string[] pathSegments)
        {
            if (pathSegments.Any(segment => !IsSafePathSegment(segment) && !IsSafeFileName(segment)))
            {
                return null;
            }

            var assetRoot = GetAssetRootPath(configuredPath);
            if (assetRoot is null)
            {
                return null;
            }

            var rootPath = Path.GetFullPath(assetRoot);
            var requestedPath = Path.GetFullPath(Path.Combine([rootPath, .. pathSegments]));
            var relativePath = Path.GetRelativePath(rootPath, requestedPath);

            return relativePath.Equals("..", StringComparison.Ordinal)
                || relativePath.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                || relativePath.StartsWith($"..{Path.AltDirectorySeparatorChar}", StringComparison.Ordinal)
                || Path.IsPathRooted(relativePath)
                    ? null
                    : requestedPath;
        }

        private static string? GetAssetRootPath(string configuredPath)
        {
            if (Directory.Exists(configuredPath))
            {
                return configuredPath;
            }

            return Path.GetDirectoryName(configuredPath);
        }

        private static string GetAssetRouteBase(string mappedPublicUrlPath)
        {
            var trimmedPath = mappedPublicUrlPath.TrimEnd('/');
            var routeFileName = Path.GetFileName(trimmedPath);

            if (!SupportedContentTypes.ContainsKey(Path.GetExtension(routeFileName)))
            {
                return trimmedPath;
            }

            var lastSlashIndex = trimmedPath.LastIndexOf('/');
            return lastSlashIndex <= 0 ? string.Empty : trimmedPath[..lastSlashIndex];
        }

        private static IEnumerable<ProtomapsGlyphEntry> GetAvailableGlyphs(ProtomapsOptions protomapsOptions, string assetRouteBase)
        {
            var assetRoot = GetAssetRootPath(protomapsOptions.FilePath);
            if (string.IsNullOrWhiteSpace(assetRoot))
            {
                yield break;
            }

            var fontsRoot = Path.Combine(assetRoot, "fonts");
            if (!Directory.Exists(fontsRoot))
            {
                yield break;
            }

            foreach (var fontDirectory in Directory.EnumerateDirectories(fontsRoot))
            {
                var fontstack = Path.GetFileName(fontDirectory);
                if (!IsSafePathSegment(fontstack))
                {
                    continue;
                }

                var ranges = Directory.EnumerateFiles(fontDirectory, "*.pbf")
                    .Select(Path.GetFileName)
                    .Where(rangeFile => rangeFile is not null && IsSafeFileName(rangeFile))
                    .OrderBy(rangeFile => rangeFile, StringComparer.OrdinalIgnoreCase)
                    .Cast<string>()
                    .ToArray();

                if (ranges.Length == 0)
                {
                    continue;
                }

                yield return new ProtomapsGlyphEntry(
                    fontstack,
                    ranges,
                    $"{assetRouteBase}/fonts/{Uri.EscapeDataString(fontstack)}/{{range}}.pbf");
            }
        }

        private static IEnumerable<string> GetAvailableSpriteVersions(ProtomapsOptions protomapsOptions)
        {
            var assetRoot = GetAssetRootPath(protomapsOptions.FilePath);
            if (string.IsNullOrWhiteSpace(assetRoot))
            {
                yield break;
            }

            var spritesRoot = Path.Combine(assetRoot, "sprites");
            if (!Directory.Exists(spritesRoot))
            {
                yield break;
            }

            foreach (var versionDirectory in Directory.EnumerateDirectories(spritesRoot))
            {
                var version = Path.GetFileName(versionDirectory);
                if (!IsSafePathSegment(version))
                {
                    continue;
                }

                var hasSupportedSprites = Directory.EnumerateFiles(versionDirectory)
                    .Any(filePath => SupportedSpriteContentTypes.ContainsKey(Path.GetExtension(filePath)));

                if (!hasSupportedSprites)
                {
                    continue;
                }

                yield return version;
            }
        }

        private static IEnumerable<ProtomapsFileEntry> GetAvailableFiles(string mappedPublicUrlPath, ProtomapsOptions protomapsOptions)
        {
            if (Directory.Exists(protomapsOptions.FilePath))
            {
                foreach (var filePath in Directory.EnumerateFiles(protomapsOptions.FilePath))
                {
                    if (!SupportedContentTypes.ContainsKey(Path.GetExtension(filePath)))
                    {
                        continue;
                    }

                    var fileInfo = new FileInfo(filePath);
                    yield return CreateFileEntry(fileInfo, AppendPath(mappedPublicUrlPath, fileInfo.Name), protomapsOptions);
                }

                yield break;
            }

            if (!SupportedContentTypes.ContainsKey(Path.GetExtension(protomapsOptions.FilePath)))
            {
                yield break;
            }

            var configuredFileInfo = new FileInfo(protomapsOptions.FilePath);
            var routeFileName = Path.GetFileName(mappedPublicUrlPath.TrimEnd('/'));
            var url = SupportedContentTypes.ContainsKey(Path.GetExtension(routeFileName))
                ? mappedPublicUrlPath
                : AppendPath(mappedPublicUrlPath, configuredFileInfo.Name);

            yield return CreateFileEntry(configuredFileInfo, url, protomapsOptions);
        }

        private static ProtomapsFileEntry CreateFileEntry(FileInfo fileInfo, string url, ProtomapsOptions protomapsOptions)
        {
            var extension = fileInfo.Extension.ToLowerInvariant();

            return new ProtomapsFileEntry(
                fileInfo.Name,
                extension.TrimStart('.'),
                url,
                GetContentType(fileInfo.FullName, protomapsOptions),
                fileInfo.Exists ? fileInfo.Length : null,
                fileInfo.Exists ? fileInfo.LastWriteTimeUtc : null);
        }

        private static bool IsInvalidPmtilesArchive(string filePath)
        {
            if (!string.Equals(Path.GetExtension(filePath), ".pmtiles", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            Span<byte> header = stackalloc byte[7];
            using var stream = File.OpenRead(filePath);

            return stream.Read(header) != header.Length || !header.SequenceEqual("PMTiles"u8);
        }

        private static string GetContentType(string filePath, ProtomapsOptions protomapsOptions)
        {
            var extension = Path.GetExtension(filePath);

            if (string.Equals(extension, ".pmtiles", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(protomapsOptions.ContentType))
            {
                return protomapsOptions.ContentType;
            }

            return SupportedContentTypes.TryGetValue(extension, out var contentType)
                ? contentType
                : "application/octet-stream";
        }

        private static bool IsSafeFileName(string fileName)
        {
            return IsSafePathSegment(fileName)
                && string.Equals(fileName, Path.GetFileName(fileName), StringComparison.Ordinal);
        }

        private static bool IsSafePathSegment(string segment)
        {
            return !string.IsNullOrWhiteSpace(segment)
                && !segment.Contains("..", StringComparison.Ordinal)
                && segment.IndexOfAny(Path.GetInvalidFileNameChars()) < 0;
        }

        private static string AppendPath(string basePath, string fileName)
        {
            return $"{basePath.TrimEnd('/')}/{fileName}";
        }

        private sealed record ProtomapsFileOverview(
            IReadOnlyCollection<ProtomapsFileEntry> Files,
            IReadOnlyCollection<ProtomapsGlyphEntry> Glyphs,
            IReadOnlyCollection<string> SpriteVersions);

        private sealed record ProtomapsFileEntry(string Name, string Type, string Url, string ContentType, long? SizeBytes, DateTimeOffset? LastModifiedUtc);

        private sealed record ProtomapsGlyphEntry(string Fontstack, IReadOnlyCollection<string> Ranges, string UrlTemplate);
    }
}
