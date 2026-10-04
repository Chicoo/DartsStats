# Protomaps Hosting Service Design

## Overview
`Protomaps.Host` is a dedicated ASP.NET Core service that exposes `.pmtiles`, `.maptiles`, and `.geojson` artifacts at stable URLs and supports byte-range access for efficient tile retrieval by frontend map clients.

## Components
- `Protomaps.Host`
: HTTP server exposing health, file overview, and map data endpoints.
- `Protomaps.AppHost`
: Aspire orchestration project for local run and container artifact generation.
- `Protomaps.Demo`
: Angular + MapLibre local validation client using PMTiles protocol plugin.
- Hosted data file
: One configured absolute map data file path, or one configured absolute directory containing supported map data files.

## Request Flow
1. Client requests `GET /protomaps/main.pmtiles` (or configured `PublicUrlPath`).
2. Service validates startup configuration state.
3. Service checks configured file existence and read accessibility.
4. Service streams file with range processing enabled.
5. Service applies response headers (`Content-Type`, `Cache-Control`).

## File Overview Flow
1. Client requests `GET /protomaps/files` when `PublicUrlPath` is `/protomaps`.
2. Service enumerates the configured directory, or returns the single configured file.
3. Service filters to supported extensions: `.pmtiles`, `.maptiles`, and `.geojson`.
4. Service returns file names, types, request URLs, content types, sizes, and UTC modification timestamps.

## Frontend Protocol Flow
1. Frontend reads configured HTTP/HTTPS PMTiles URL.
2. Frontend registers PMTiles protocol with MapLibre.
3. Frontend uses `pmtiles://<http-url>` for vector source loading.
4. Frontend requests ranged bytes from `Protomaps.Host` over HTTP(S).

`Protomaps.Host` does not implement a `pmtiles://` server protocol endpoint. It serves HTTP(S) bytes only; `pmtiles://` interpretation is client-side behavior in MapLibre + PMTiles library.

## Range Request Flow
1. Client sends `Range` header (for example `bytes=0-65535`).
2. ASP.NET Core file result handles partial-content processing.
3. Service returns `206 Partial Content` with `Content-Range` when valid.

## Security Model
- No static-file middleware is used.
- No directory path is exposed.
- Directory mode accepts only safe file names, not user-supplied paths.
- Endpoint path is configured as a fixed literal route only.
- Configured file path must be absolute and must target a supported map data file or directory.
- Service account permissions should be read-only for the target file or directory.

## CORS Model
- Disabled by default.
- If enabled, explicit origin allowlist is required.
- Non-allowlisted origins do not receive CORS allow headers.

## Failure Modes
- Invalid config: `500` with clear problem details.
- File missing: `404` with clear problem details.
- Access denied: `403` with clear problem details.
- Read failure: `500` with clear problem details and server log entry.

## Recovery
- Correct invalid config and restart service.
- Restore or replace missing map data files at the configured path.
- Fix file permissions for service identity.
