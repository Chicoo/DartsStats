# Protomaps Hosting Service

Standalone Protomaps hosting and demo solution.

## Projects
- `Protomaps.Host`: HTTP server exposing PMTiles/map data files, glyph and sprite assets, `/protomaps/files`, and `/healthz`. Deployments can configure `Protomaps:RequiredFiles`; health then fails until every required air-gapped asset exists.
- `Protomaps.AppHost`: Aspire orchestration and publish root.
- `Protomaps.Host.Tests`: integration tests for endpoint behavior.
- `Protomaps.Demo`: minimal Angular + MapLibre frontend demo using PMTiles protocol and local glyph/sprite endpoints.

## Run
From `Full Demo`, run `aspire run` to launch this service with the DartsStats AppHost.
The dashboard includes the `protomaps` resource, its `/healthz` health check,
telemetry, and a **Map Files** link to `/protomaps/files`.
The `protomaps-demo` Angular app is grouped under the host in the dashboard;
open its URL to preview maps. Its development server proxies map requests to
the Aspire-managed Protomaps endpoint.

Map assets default to `Full Demo/data/protomaps`. Place PMTiles files, fonts, and
sprites there, or override `Parameters:protomaps-file-path` in the DartsStats
AppHost configuration with an absolute directory or PMTiles file path. An empty
directory allows the service to start but does not provide map data.

```bash
aspire run
```

## Venue overview map

The Full Demo venue panel uses MapLibre and the local `main.pmtiles` basemap.
The AppHost supplies `PROTOMAPS_HTTP` to the React frontend; Vite proxies
`/protomaps` to that service, including range requests, fonts, and sprites.
Production hosting must route `/protomaps` to the Protomaps service and preserve
HTTP Range headers and partial-content responses. No external basemap is used.

The map starts at zoom 14 or the archive maximum, whichever is lower. The
included archive supports zoom 0–9, so it shows regional context rather than
street-level detail. Missing data or unsupported WebGL shows an unavailable
message while the venue details remain usable.

## Test
```bash
dotnet test webservices/Protomaps/Protomaps.Host.Tests/Protomaps.Host.Tests.csproj
```

## Asset Layout
Glyphs and sprites must live beside the configured PMTiles data:

```text
<map-data-root>/
  main.pmtiles
  fonts/
    Noto Sans Bold/
      0-255.pbf
    Noto Sans Medium/
      0-255.pbf
    Noto Sans Regular/
      0-255.pbf
  sprites/
    v4/
      light.json
      light.png
      light@2x.json
      light@2x.png
```

If `Protomaps:FilePath` points to a directory, that directory is the asset root.
If `Protomaps:FilePath` points to a single `.pmtiles` file, the parent directory of that file is the asset root.

## Endpoints
- `GET /healthz`
- `GET /protomaps/main.pmtiles`
- `GET /protomaps/files`
- `GET /protomaps/{fileName}`
- `GET /protomaps/fonts/{fontstack}/{range}.pbf`
- `GET /protomaps/sprites/{version}/{spriteFile}`

Examples:

```bash
curl http://localhost:5138/healthz
curl http://localhost:5138/protomaps/files
curl -H "Range: bytes=0-1023" http://localhost:5138/protomaps/main.pmtiles
curl http://localhost:5138/protomaps/fonts/Noto%20Sans%20Bold/0-255.pbf
curl http://localhost:5138/protomaps/fonts/Noto%20Sans%20Regular/0-255.pbf
curl http://localhost:5138/protomaps/sprites/v4/light.json
curl http://localhost:5138/protomaps/sprites/v4/light.png
```

## Load Test
The Protomaps load test is a manual benchmark and is not part of CI. It sends PMTiles byte-range requests to a running Protomaps service at `100`, `1000`, and `10000` requests per second by default.

Start the Protomaps service locally or deploy it to the environment you want to measure, then run:

```powershell
dotnet run --project webservices/Protomaps/Protomaps.Host.LoadTests/Protomaps.Host.LoadTests.csproj -- --base-url http://localhost:5138
```

Useful options:

```powershell
dotnet run --project webservices/Protomaps/Protomaps.Host.LoadTests/Protomaps.Host.LoadTests.csproj -- `
  --base-url http://localhost:5138 `
  --path /protomaps/main.pmtiles `
  --rates 100,1000,10000 `
  --duration-seconds 60 `
  --range bytes=0-1023 `
  --max-concurrency 2000 `
  --fail-on-errors true
```

For a quick smoke test against a running service:

```powershell
dotnet run --project webservices/Protomaps/Protomaps.Host.LoadTests/Protomaps.Host.LoadTests.csproj -- --base-url http://localhost:5138 --rates 1,2 --duration-seconds 2
```

The `10000` RPS phase requires sufficient client CPU, service capacity, and network bandwidth. Treat reported latency and throughput as environment-specific benchmark data rather than CI pass/fail thresholds.

## Frontend Demo (Unit Test + Build)
```bash
cd webservices/Protomaps/Protomaps.Demo
npm run test
npm run build
```

## Container Image Push
Build and push the self-contained Protomaps image with a zoom 0-7 PMTiles archive baked into `/data/main.pmtiles`:

```powershell
$env:NEXUS_USERNAME = "<nexus-user>"
$env:NEXUS_PASSWORD = "<nexus-password>"

aspire do push-protomaps-host `
  --apphost webservices/Protomaps/Protomaps.AppHost/Protomaps.AppHost.csproj `
  --non-interactive `
  -- --sourcePmtilesUrl "https://<source>/main.pmtiles" --pmtilesVersion "1.30.3"
```

The image is pushed as `acrtopfas.azurecr.io/topfas/protomaps-service:latest`.

The Dockerfile uses the repository root as its build context because `Protomaps.Host` references `hosting/Topfas.ServiceDefaults`. The `publish-cyber.ps1` helper configures that context automatically.

## Docker run
Run the pushed container. The PMTiles file is already baked into the image, so no PMTiles source URL or volume is required at runtime.

Run it:

```powershell
docker run --rm -p 8080:5138 protomaps-host:latest
```

Then verify:

```powershell
curl http://localhost:5138/healthz
curl http://localhost:5138/protomaps/files
curl -H "Range: bytes=0-1023" http://localhost:5138/protomaps/main.pmtiles
curl http://localhost:5138/protomaps/fonts/Noto%20Sans%20Bold/0-255.pbf
curl http://localhost:5138/protomaps/fonts/Noto%20Sans%20Regular/0-255.pbf
curl http://localhost:5138/protomaps/sprites/v4/light.json
```

## IIS Deployment (Windows Server 2022)
Use:

`webservices/Protomaps/tools/install-protomaps-iis.ps1`

Example:
```powershell
powershell -ExecutionPolicy Bypass -File webservices/Protomaps/tools/install-protomaps-iis.ps1 `
  -PublishedPackagePath "C:\deploy\Protomaps.Host.zip" `
  -PmtilesFilePath "C:\protomaps\main.pmtiles"
```

## Documentation
See `webservices/Protomaps/docs/`.
