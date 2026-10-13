# Frontend Demo (Angular + MapLibre + PMTiles)

## Purpose
`Protomaps.Demo` is a minimal local validation frontend. It verifies that the hosted PMTiles URL can be consumed by a browser map client using MapLibre + PMTiles protocol handling, with local glyph and sprite assets served by `Protomaps.Host`.

## Location
- `webservices/Protomaps/Protomaps.Demo`

## Behavior
- Input accepts an HTTP/HTTPS PMTiles URL.
- URL precedence:
  1. `?pmtilesUrl=<url>` query parameter
  2. `localStorage` saved URL
  3. default `http://localhost:5138/protomaps/main.pmtiles`
- Frontend registers PMTiles protocol:
  - `maplibregl.addProtocol("pmtiles", protocol.tile)`
- Frontend converts URL to `pmtiles://...` internally for MapLibre vector source usage.
- Demo uses `@protomaps/basemaps` to render a full Protomaps basemap style.
- Demo points MapLibre at local host endpoints for:
  - `glyphs`: `http://localhost:5138/protomaps/fonts/{fontstack}/{range}.pbf`
  - `sprite`: `http://localhost:5138/protomaps/sprites/v4/light`
- Demo renders PMTiles and GeoJSON overlays on top of the basemap.
- If no source layers are found, the status panel shows an error.

## Run
Use Aspire orchestration from the Protomaps AppHost:

```bash
aspire run --apphost webservices/Protomaps/Protomaps.AppHost/Protomaps.AppHost.csproj
```

Then open the `protomaps-demo` resource URL from the Aspire dashboard.

## Required Asset Files
The demo expects the host to serve extracted Protomaps assets beside the PMTiles file:

```text
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

## Frontend Unit Tests
```bash
cd webservices/Protomaps/Protomaps.Demo
npm run test
```

Covered:
- URL precedence resolution.
- URL normalization to `pmtiles://`.
- Local glyph/sprite endpoint constants.
- Style creation includes local `glyphs` and `sprite` URLs.
- Error state behavior when map initialization fails.
