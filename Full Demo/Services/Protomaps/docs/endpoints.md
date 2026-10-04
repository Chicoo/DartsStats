# Endpoint Contract

## Endpoints

| Method | Path | Purpose |
|---|---|---|
| `GET` | `/healthz` | Liveness/readiness probe |
| `GET` | `/protomaps/main.pmtiles` | Serves the default PMTiles file from the configured asset directory |
| `GET` | `/protomaps/files` | Lists available `.pmtiles`, `.maptiles`, and `.geojson` files, plus available glyph fontstacks and sprite versions when `PublicUrlPath` is `/protomaps` |
| `GET` | `/protomaps/{fileName}` | Serves supported files by name when `Protomaps:FilePath` points to a directory |
| `GET` | `/protomaps/fonts/{fontstack}/{range}.pbf` | Serves MapLibre glyph/fontstack assets from `fonts/` |
| `GET` | `/protomaps/sprites/{version}/{spriteFile}` | Serves MapLibre sprite JSON/PNG assets from `sprites/` |

> `PublicUrlPath` is configurable through `Protomaps:PublicUrlPath`; its default is `/protomaps`, which makes the file overview available at `/protomaps/files`.

## Asset Root Resolution
- If `Protomaps:FilePath` points to a directory, that directory is used as the asset root.
- If `Protomaps:FilePath` points to a single `.pmtiles`, `.maptiles`, or `.geojson` file, the parent directory of that file is used as the asset root.

Expected layout:

```text
<asset-root>/
  main.pmtiles
  fonts/
    Noto Sans Regular/
      0-255.pbf
  sprites/
    v4/
      light.json
      light.png
      light@2x.json
      light@2x.png
```

## `GET /healthz`
- Success: `200 OK`
- Body: framework health response

## `GET /protomaps/main.pmtiles`
### Success responses
- `200 OK` for full-file requests
- `206 Partial Content` for valid byte-range requests

### Error responses
- `403` when configured file is not readable by service identity
- `404` when configured file does not exist
- `500` when configuration is invalid or internal read fails

### Headers
- `Content-Type`: from `Protomaps:ContentType` (default `application/octet-stream`)
- `Cache-Control`: from `Protomaps:CacheControl`
- Range-related headers are emitted by ASP.NET Core range file processing
- `Access-Control-Allow-Origin`: present only when CORS is enabled and request origin is allowlisted

### Protocol clarification
- Server contract is HTTP/HTTPS only.
- `pmtiles://` is handled by the frontend map client and translated into ranged HTTP(S) requests against the stable PMTiles endpoint.

## `GET /protomaps/files`
Returns a JSON overview of available map data files.

### Success response
- `200 OK` with `application/json` body.

### Response body
```json
{
  "files": [
    {
      "name": "world.pmtiles",
      "type": "pmtiles",
      "url": "/protomaps/world.pmtiles",
      "contentType": "application/octet-stream",
      "sizeBytes": 4096,
      "lastModifiedUtc": "2026-06-07T07:45:00Z"
    },
    {
      "name": "areas.geojson",
      "type": "geojson",
      "url": "/protomaps/areas.geojson",
      "contentType": "application/geo+json",
      "sizeBytes": 42,
      "lastModifiedUtc": "2026-06-07T07:45:00Z"
    }
  ],
  "glyphs": [
    {
      "fontstack": "Noto Sans Regular",
      "ranges": ["0-255.pbf", "256-511.pbf"],
      "urlTemplate": "/protomaps/fonts/Noto%20Sans%20Regular/{range}.pbf"
    }
  ],
  "spriteVersions": ["v4"]
}
```

### Behavior
- Directory configuration lists only files with supported extensions: `.pmtiles`, `.maptiles`, and `.geojson`.
- Unsupported files in the configured directory are omitted.
- Glyph fontstacks and sprite versions are summarized in `/files`, but individual asset files are not listed there.
- Single-file configuration returns one entry for the configured file.
- Results are sorted by file name, case-insensitively.

### Error responses
- `403` when the configured path cannot be listed by the service identity.
- `500` when listing fails unexpectedly.

## `GET /protomaps/{fileName}`
When `Protomaps:FilePath` points to a directory, supported files can be requested by file name.

### Supported extensions
- `.pmtiles`
- `.maptiles`
- `.geojson`

### Error responses
- `404` when the file name is unsupported or the file does not exist.
- `415` when a `.pmtiles` file does not contain a valid PMTiles archive header.
- Other file-read failures match the single-file endpoint behavior.

## `GET /protomaps/fonts/{fontstack}/{range}.pbf`
Serves precompiled MapLibre glyph/fontstack ranges from `fonts/{fontstack}/{range}.pbf`.

### Success responses
- `200 OK`

### Headers
- `Content-Type: application/x-protobuf`
- `Cache-Control`: from `Protomaps:CacheControl`

### Error responses
- `404` when the asset does not exist or the request path is unsafe.
- `403` when the asset is not readable by the service identity.
- `500` when an internal read fails.

## `GET /protomaps/sprites/{version}/{spriteFile}`
Serves sprite assets from `sprites/{version}`.

### Supported files
- `*.json`
- `*.png`
- `*@2x.json`
- `*@2x.png`

### Headers
- `Content-Type: application/json` for `.json`
- `Content-Type: image/png` for `.png`
- `Cache-Control`: from `Protomaps:CacheControl`

### Error responses
- `404` when the asset does not exist, the extension is unsupported, or the request path is unsafe.
- `403` when the asset is not readable by the service identity.
- `500` when an internal read fails.

## Request/Response Examples

### Full file
```bash
curl -i http://localhost:5138/protomaps/main.pmtiles
```

Expected:
- `HTTP/1.1 200 OK`
- `Content-Type: application/octet-stream` (or configured override)

### Byte range
```bash
curl -i -H "Range: bytes=0-1023" http://localhost:5138/protomaps/main.pmtiles
```

Expected:
- `HTTP/1.1 206 Partial Content`
- `Content-Range: bytes 0-1023/<full-size>`

### CORS check
```bash
curl -i -H "Origin: https://frontend.example" http://localhost:5138/protomaps/main.pmtiles
```

Expected when allowlisted:
- `Access-Control-Allow-Origin: https://frontend.example`

### File overview
```bash
curl -i http://localhost:5138/protomaps/files
```

Expected:
- `HTTP/1.1 200 OK`
- JSON body with `files`, `glyphs`, and `spriteVersions` arrays

### Glyph asset
```bash
curl -i http://localhost:5138/protomaps/fonts/Noto%20Sans%20Regular/0-255.pbf
```

### Sprite assets
```bash
curl -i http://localhost:5138/protomaps/sprites/v4/light.json
curl -i http://localhost:5138/protomaps/sprites/v4/light.png
```
