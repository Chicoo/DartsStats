const hostOrigin = typeof window === 'undefined' ? 'http://localhost:5138' : window.location.origin;
export const PROTOMAPS_BASE_URL = `${hostOrigin}/protomaps`;
export const DEFAULT_PM_TILES_HTTP_URL = `${PROTOMAPS_BASE_URL}/main.pmtiles`;
export const DEFAULT_GLYPHS_HTTP_URL = `${PROTOMAPS_BASE_URL}/fonts/{fontstack}/{range}.pbf`;
export const DEFAULT_SPRITE_HTTP_URL = `${PROTOMAPS_BASE_URL}/sprites/v4/light`;
export const DEFAULT_OVERLAY_PM_TILES_HTTP_URLS: string[] = [];
export const DEFAULT_GEOJSON_HTTP_URLS = [
  `${PROTOMAPS_BASE_URL}/boundaries.geojson`,
  `${PROTOMAPS_BASE_URL}/capitals.geojson`,
  `${PROTOMAPS_BASE_URL}/countries.geojson`,
  `${PROTOMAPS_BASE_URL}/nations.geojson`,
];
export const PMTILES_URL_STORAGE_KEY = 'protomaps.demo.pmtiles-url';
