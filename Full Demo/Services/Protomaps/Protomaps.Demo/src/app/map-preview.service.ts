import { Injectable } from '@angular/core';
import { layers as protomapsLayers, namedFlavor } from '@protomaps/basemaps';
import maplibregl, {
  type FilterSpecification,
  type GeoJSONSourceSpecification,
  type LayerSpecification,
  type SourceSpecification,
  type StyleSpecification,
  type VectorSourceSpecification,
} from 'maplibre-gl';
import { PMTiles, Protocol } from 'pmtiles';
import {
  DEFAULT_GEOJSON_HTTP_URLS,
  DEFAULT_GLYPHS_HTTP_URL,
  DEFAULT_OVERLAY_PM_TILES_HTTP_URLS,
  DEFAULT_SPRITE_HTTP_URL,
} from './map-url.constants';
import { toPmtilesProtocolUrl } from './map-url';

type VectorLayerMetadata = {
  id?: string;
};

type PmtilesMetadata = {
  vector_layers?: VectorLayerMetadata[];
};

type VectorSourceConfig = {
  id: string;
  url: string;
  sourceLayers: string[];
  isOverlay: boolean;
};

type GeoJsonSourceConfig = {
  id: string;
  url: string;
};

export type MapLoadResult = {
  sourceUrl: string;
  availableLayers: string[];
  overlayUrls: string[];
  geoJsonUrls: string[];
  skippedOverlayUrls: string[];
};

const mapProtocol = new Protocol();
let protocolRegistered = false;

@Injectable({
  providedIn: 'root',
})
export class MapPreviewService {
  private map?: maplibregl.Map;

  public async load(container: HTMLDivElement, pmtilesHttpUrl: string): Promise<MapLoadResult> {
    this.destroy();
    ensureProtocolRegistration();

    const baseSource = await loadVectorSource('protomaps', pmtilesHttpUrl, false);

    if (baseSource.sourceLayers.length === 0) {
      throw new Error('No vector layers found in PMTiles metadata.');
    }

    const overlayLoadResults = await Promise.all(
      DEFAULT_OVERLAY_PM_TILES_HTTP_URLS.map(async (url, index) => {
        try {
          return {
            source: await loadVectorSource(`pmtiles-overlay-${index}`, url, true),
            skippedUrl: null,
          };
        } catch {
          return {
            source: null,
            skippedUrl: url,
          };
        }
      }),
    );
    const overlaySources = overlayLoadResults
      .map((result) => result.source)
      .filter((source): source is VectorSourceConfig => source !== null && source.sourceLayers.length > 0);
    const skippedOverlayUrls = overlayLoadResults
      .map((result) => result.skippedUrl)
      .filter((url): url is string => url !== null);
    const geoJsonSources = DEFAULT_GEOJSON_HTTP_URLS.map((url, index) => ({
      id: `geojson-overlay-${index}`,
      url,
    }));
    const availableLayers = [...new Set(baseSource.sourceLayers)].sort((left, right) =>
      left.localeCompare(right),
    );

    const style = createStyle(baseSource, overlaySources, geoJsonSources);
    const map = new maplibregl.Map({
      container,
      center: [6.13, 52.24],
      zoom: 4,
      style,
    });
    this.map = map;

    return await new Promise<MapLoadResult>((resolve, reject) => {
      map.once('error', (event) => {
        reject(new Error(event.error?.message ?? 'Unknown map rendering error.'));
      });

      map.once('load', () => {
        resolve({
          sourceUrl: pmtilesHttpUrl,
          availableLayers,
          overlayUrls: overlaySources.map((source) => source.url),
          geoJsonUrls: geoJsonSources.map((source) => source.url),
          skippedOverlayUrls,
        });
      });
    });
  }

  public destroy(): void {
    if (this.map) {
      this.map.remove();
      this.map = undefined;
    }
  }
}

function ensureProtocolRegistration(): void {
  if (protocolRegistered) {
    return;
  }

  maplibregl.addProtocol('pmtiles', mapProtocol.tile);
  protocolRegistered = true;
}

async function loadVectorSource(
  id: string,
  pmtilesHttpUrl: string,
  isOverlay: boolean,
): Promise<VectorSourceConfig> {
  const pmtiles = new PMTiles(pmtilesHttpUrl);
  mapProtocol.add(pmtiles);
  const metadata = (await pmtiles.getMetadata()) as PmtilesMetadata;
  const sourceLayers =
    metadata.vector_layers
      ?.map((layer) => layer.id)
      .filter((layer): layer is string => typeof layer === 'string' && layer.length > 0) ?? [];

  return {
    id,
    url: pmtilesHttpUrl,
    sourceLayers: [...new Set(sourceLayers)].sort((left, right) => left.localeCompare(right)),
    isOverlay,
  };
}

export function createStyle(
  baseSource: VectorSourceConfig,
  overlaySources: VectorSourceConfig[],
  geoJsonSources: GeoJsonSourceConfig[],
): StyleSpecification {
  const sources: Record<string, SourceSpecification> = {
    [baseSource.id]: {
      type: 'vector',
      url: toPmtilesProtocolUrl(baseSource.url),
      attribution:
        '<a href="https://protomaps.com">Protomaps</a> © <a href="https://openstreetmap.org">OpenStreetMap</a>',
    } satisfies VectorSourceSpecification,
  };

  const layers: LayerSpecification[] = [...protomapsLayers(baseSource.id, namedFlavor('light'), { lang: 'en' })];

  for (const source of overlaySources) {
    sources[source.id] = {
      type: 'vector',
      url: toPmtilesProtocolUrl(source.url),
    } satisfies VectorSourceSpecification;

    layers.push(...createVectorLayers(source));
  }

  for (const source of geoJsonSources) {
    sources[source.id] = {
      type: 'geojson',
      data: source.url,
    } satisfies GeoJSONSourceSpecification;

    layers.push(...createGeoJsonLayers(source));
  }

  return {
    version: 8,
    glyphs: DEFAULT_GLYPHS_HTTP_URL,
    sprite: DEFAULT_SPRITE_HTTP_URL,
    sources,
    layers,
  };
}

function createVectorLayers(source: VectorSourceConfig): LayerSpecification[] {
  const layers: LayerSpecification[] = [];

  for (const sourceLayer of source.sourceLayers) {
    const safeLayerId = `${source.id}-${sourceLayer}`.replace(/[^a-zA-Z0-9_-]/g, '_');
    const color = source.isOverlay ? '#e11d48' : layerColor(sourceLayer);
    const fillOpacity = source.isOverlay ? 0.28 : 0.16;
    const lineWidth = source.isOverlay ? 2 : 1;
    const pointRadius = source.isOverlay ? 4 : 2.5;

    layers.push({
      id: `fill-${safeLayerId}`,
      type: 'fill',
      source: source.id,
      'source-layer': sourceLayer,
      filter: geometryFilter('Polygon'),
      paint: {
        'fill-color': color,
        'fill-opacity': fillOpacity,
      },
    });

    layers.push({
      id: `line-${safeLayerId}`,
      type: 'line',
      source: source.id,
      'source-layer': sourceLayer,
      filter: geometryFilter('LineString'),
      paint: {
        'line-color': color,
        'line-opacity': 0.9,
        'line-width': lineWidth,
      },
    });

    layers.push({
      id: `point-${safeLayerId}`,
      type: 'circle',
      source: source.id,
      'source-layer': sourceLayer,
      filter: geometryFilter('Point'),
      paint: {
        'circle-color': color,
        'circle-radius': pointRadius,
        'circle-opacity': 0.85,
      },
    });
  }

  return layers;
}

function createGeoJsonLayers(source: GeoJsonSourceConfig): LayerSpecification[] {
  const color = layerColor(source.url);

  return [
    {
      id: `${source.id}-fill`,
      type: 'fill',
      source: source.id,
      filter: geometryFilter('Polygon'),
      paint: {
        'fill-color': color,
        'fill-opacity': 0.1,
      },
    },
    {
      id: `${source.id}-line`,
      type: 'line',
      source: source.id,
      filter: geometryFilter('LineString'),
      paint: {
        'line-color': color,
        'line-opacity': 0.75,
        'line-width': 1.5,
      },
    },
    {
      id: `${source.id}-point`,
      type: 'circle',
      source: source.id,
      filter: geometryFilter('Point'),
      paint: {
        'circle-color': color,
        'circle-radius': 3.5,
        'circle-opacity': 0.85,
      },
    },
  ];
}

function geometryFilter(type: 'Point' | 'LineString' | 'Polygon'): FilterSpecification {
  return ['==', ['geometry-type'], type];
}

function layerColor(layerName: string): string {
  let hash = 0;
  for (let index = 0; index < layerName.length; index += 1) {
    hash = ((hash << 5) - hash + layerName.charCodeAt(index)) | 0;
  }

  const hue = Math.abs(hash) % 360;
  return `hsl(${hue} 60% 42%)`;
}
