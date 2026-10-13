# ProtomapsDemo

Angular demo app for validating the local Protomaps host.

## What it validates
- PMTiles loading through `pmtiles://` protocol handling.
- Local glyph endpoint: `http://localhost:5138/protomaps/fonts/{fontstack}/{range}.pbf`
- Local sprite endpoint: `http://localhost:5138/protomaps/sprites/v4/light`
- Protomaps basemap rendering via `@protomaps/basemaps`.
- Optional PMTiles and GeoJSON overlays.

## Development server

Run `aspire run` from `Full Demo` and open the `protomaps-demo` dashboard link.
Aspire installs dependencies, assigns the demo port, and starts it after the
Protomaps host is healthy. The Angular development server proxies `/protomaps`
requests to that host, including PMTiles ranges, fonts, sprites, and overlays.

To start a local development server, run:

```bash
npm start
```

Once the server is running, open `http://localhost:4212/`. Standalone mode proxies
to `http://localhost:5138`; set `PROTOMAPS_HTTP` to use another host endpoint.

## Building

To build the project run:

```bash
ng build
```

## Running unit tests

To execute unit tests with the Vitest test runner, use the following command:

```bash
ng test
```

## Required host assets
The host must expose assets with this layout beside the PMTiles file:

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
