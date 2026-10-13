# Deployment and Installation

## Prerequisites
- For local development:
  - .NET SDK 10+
  - Aspire CLI installed
- For container image push:
  - Docker Desktop (or another supported container runtime)
  - Azure CLI installed and logged in
  - `AcrPush` permission on `acrtopfas`
  - `NEXUS_USERNAME` and `NEXUS_PASSWORD` available for Docker build restore
- Source `.pmtiles` URL reachable from Docker during build; the publisher passes it as a BuildKit secret so signed URLs are not retained as image build arguments
- For IIS target deployment:
  - Windows Server 2022
  - PowerShell (run as Administrator)
  - Published `Protomaps.Host` package (folder or `.zip`)
  - Access to target `.pmtiles` absolute file path
  - Network exposure for target HTTP/HTTPS endpoint

## Local Run (Aspire)
```bash
aspire run --apphost webservices/Protomaps/Protomaps.AppHost/Protomaps.AppHost.csproj
```

This AppHost starts:
- `protomaps-host` (`Protomaps.Host`) on `http://localhost:5138` by default.
- `protomaps-demo` (`Protomaps.Demo` Angular client) on `http://localhost:4200`.

Local AppHost configuration enables CORS on the host service for `http://localhost:4200`.

## Canonical Container Image Push (Aspire CLI)
```powershell
$env:NEXUS_USERNAME = "<nexus-user>"
$env:NEXUS_PASSWORD = "<nexus-password>"

aspire do push-protomaps-host `
  --apphost webservices/Protomaps/Protomaps.AppHost/Protomaps.AppHost.csproj `
  --non-interactive `
  -- --sourcePmtilesUrl "https://<source>/main.pmtiles" --pmtilesVersion "1.30.3"
```

This builds `Protomaps.Host` with a zoom 0-7 PMTiles archive baked into `/data/main.pmtiles` and pushes `acrtopfas.azurecr.io/topfas/protomaps-service:latest`.

The Docker build:
- Downloads the pinned `go-pmtiles` CLI version.
- Uses the repository root as its context so the shared `hosting/Topfas.ServiceDefaults` project is available.
- Restores .NET packages using `NEXUS_USERNAME` and `NEXUS_PASSWORD` as BuildKit secrets.
- Runs `pmtiles extract <source> /data/main.pmtiles --maxzoom=7`.
- Runs `pmtiles verify /data/main.pmtiles`.
- Bakes the pinned Protomaps basemap glyph and sprite assets into `/data`.
- Has no Azure or public-network dependency at container runtime; air-gapped clusters only need the final OCI image.
- Fails the build if the baked PMTiles header reports `maxzoom` greater than `7`.

The source archive must be clustered and reachable from inside the Docker build.

## IIS Deployment Script (Windows Server 2022)
Primary deployment path for IIS is the automation script:

`webservices/Protomaps/tools/install-protomaps-iis.ps1`

### Script responsibilities
- Installs IIS role/services (unless skipped)
- Installs/repairs ASP.NET Core Hosting Bundle (unless skipped)
- Deploys published package to IIS physical path
- Creates/updates App Pool + Site + Bindings
- Sets required app pool environment variables
- Grants read permissions to PMTiles and site paths
- Restarts IIS resources and runs smoke tests (`/healthz`, full request, range request)

### Example usage
```powershell
powershell -ExecutionPolicy Bypass -File webservices/Protomaps/tools/install-protomaps-iis.ps1 `
  -PublishedPackagePath "C:\deploy\Protomaps.Host.zip" `
  -PmtilesFilePath "C:\protomaps\main.pmtiles" `
  -SiteName "ProtomapsHost" `
  -AppPoolName "ProtomapsHostPool" `
  -Port 80
```

### HTTPS example
```powershell
powershell -ExecutionPolicy Bypass -File webservices/Protomaps/tools/install-protomaps-iis.ps1 `
  -PublishedPackagePath "C:\deploy\Protomaps.Host.zip" `
  -PmtilesFilePath "C:\protomaps\main.pmtiles" `
  -UseHttps `
  -Port 443 `
  -CertificateThumbprint "<thumbprint>" `
  -HostHeader "maps.example.com"
```

## Runtime Configuration
Set through environment variables or configuration files:
- `Protomaps__FilePath`
- `Protomaps__PublicUrlPath`
- `Protomaps__ContentType`
- `Protomaps__CacheControl`
- `Cors__Enabled`
- `Cors__AllowedOrigins__0`, `Cors__AllowedOrigins__1`, ...

## PMTiles file naming
- You can rename downloaded files such as `20260601.pmtiles` to `main.pmtiles`.
- Recommended operational pattern is to keep the externally documented URL stable (`/protomaps/main.pmtiles`) and replace the file content during updates.

## HTTPS and Reverse Proxy
- Service can run HTTP internally.
- TLS termination can be handled by ingress/reverse proxy.
- Ensure external URL remains stable and documented.

## Target Environment Setup Steps
1. Publish `Protomaps.Host` to a folder or zip package.
2. Place `.pmtiles` file in a controlled location.
3. Run `webservices/Protomaps/tools/install-protomaps-iis.ps1` with required parameters.
4. Configure optional CORS allowlist (`-EnableCors -AllowedOrigins ...`) when frontend origin differs.
5. Verify full and ranged requests.
6. Record evidence for work item/release attachment.

## Frontend Demo Validation
- Use `webservices/Protomaps/docs/frontend-demo.md` to validate map loading behavior against the deployed PMTiles URL.
