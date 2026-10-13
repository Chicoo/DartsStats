import { describe, expect, it } from 'vitest';
import { loadMapStatus, loadMapWithStatus } from './map-loader';

describe('loadMapStatus', () => {
  it('returns success status when the map loads', async () => {
    const fakeMapService = {
      load: async (_container: HTMLDivElement, _url: string) => ({
        sourceUrl: 'http://localhost:5138/protomaps/main.pmtiles',
        availableLayers: ['boundaries', 'places', 'roads'],
        overlayUrls: [],
        geoJsonUrls: ['http://localhost:5138/protomaps/boundaries.geojson'],
        skippedOverlayUrls: [],
      }),
    };

    const status = await loadMapStatus(
      fakeMapService,
      {} as HTMLDivElement,
      'http://localhost/map.pmtiles',
    );

    expect(status.level).toBe('success');
    expect(status.message).toMatch(/Loaded http:\/\/localhost:5138\/protomaps\/main\.pmtiles/);
  });

  it('returns error status when map initialization fails', async () => {
    const fakeMapService = {
      load: async () => {
        throw new Error('Synthetic map failure');
      },
    };

    const status = await loadMapStatus(
      fakeMapService,
      {} as HTMLDivElement,
      'http://localhost/map.pmtiles',
    );

    expect(status.level).toBe('error');
    expect(status.message).toContain('Map initialization failed');
    expect(status.message).toContain('Synthetic map failure');
  });

  it('returns source layers when loading succeeds', async () => {
    const fakeMapService = {
      load: async (_container: HTMLDivElement, _url: string) => ({
        sourceUrl: 'http://localhost:5138/protomaps/main.pmtiles',
        availableLayers: ['boundaries', 'places', 'roads'],
        overlayUrls: [],
        geoJsonUrls: ['http://localhost:5138/protomaps/boundaries.geojson'],
        skippedOverlayUrls: [],
      }),
    };

    const result = await loadMapWithStatus(
      fakeMapService,
      {} as HTMLDivElement,
      'http://localhost/map.pmtiles',
    );

    expect(result.status.level).toBe('success');
    expect(result.mapResult?.availableLayers).toEqual(['boundaries', 'places', 'roads']);
  });
});
