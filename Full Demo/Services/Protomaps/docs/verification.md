# Verification Checklist and Evidence Template

## AC-by-AC Validation Checklist

| Acceptance Criterion | Validation Method | Result | Evidence Ref |
|---|---|---|---|
| Web server installed and running in target environment | Service process/container health check |  |  |
| Protomaps `.pmtiles` hosted in documented location | Verify configured absolute file path |  |  |
| File accessible via stable HTTP/HTTPS URL | `curl` or browser request to stable URL |  |  |
| Web server returns file with appropriate headers | Capture response headers (`Content-Type`, `Cache-Control`) |  |  |
| CORS configured when required by frontend deployment | Origin-based request checks |  |  |
| File permissions restrict unrelated server files | Ops permission review |  |  |
| Large downloads or range requests work without server errors | Range and large-read tests (`206`) |  |  |
| Frontend map component loads map data from configured URL | Frontend integration smoke test |  |  |
| Installation/configuration steps documented | Documentation review |  |  |
| Tested in target environment with evidence attached | Evidence package upload to work item/release artifact |  |  |

## Evidence Collection Template
- Environment:
- Build/commit:
- Deployment timestamp:
- Stable URL:
- Config snapshot (sanitized):
- Full request output (`200`) attachment:
- Range request output (`206`) attachment:
- Header capture attachment:
- CORS allowlist test attachment:
- Frontend map load screenshot/log attachment:
- Permission review notes:
- Tester name/date:

## Suggested Commands
```bash
# full request
curl -i <stable-url>

# range request
curl -i -H "Range: bytes=0-65535" <stable-url>

# CORS verification
curl -i -H "Origin: https://frontend.example" <stable-url>
```

## Frontend Demo Manual Validation
1. Run AppHost:
   - `aspire run --apphost webservices/Protomaps/Protomaps.AppHost/Protomaps.AppHost.csproj`
2. Open the `protomaps-demo` resource URL from the Aspire dashboard.
3. Confirm PMTiles URL is populated with:
   - `http://localhost:5138/protomaps/main.pmtiles` (or environment-specific URL).
4. Click **Load map**.
5. Capture:
   - Browser network entries for PMTiles requests (including ranged responses).
   - Browser console (no blocking CORS/range errors).
   - Screenshot showing rendered map and status panel.

## IIS Script Validation (Windows Server 2022)
When deployed with `webservices/Protomaps/tools/install-protomaps-iis.ps1`, capture:
- Script execution transcript/log output.
- Final script summary output (site URL, status codes, `Content-Range` value).
- IIS site/app pool screenshot or exported configuration snippet.
