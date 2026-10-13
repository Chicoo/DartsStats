import type { MapLoadResult, MapPreviewService } from './map-preview.service';

export type UiStatus = {
  level: 'info' | 'success' | 'error';
  message: string;
};

export type LoadMapUiResult = {
  status: UiStatus;
  mapResult: MapLoadResult | null;
};

export async function loadMapWithStatus(
  mapPreviewService: Pick<MapPreviewService, 'load'>,
  container: HTMLDivElement,
  pmtilesHttpUrl: string,
): Promise<LoadMapUiResult> {
  try {
    const result = await mapPreviewService.load(container, pmtilesHttpUrl.trim());
    return {
      status: mapLoadResultToStatus(result),
      mapResult: result,
    };
  } catch (error) {
    const message = error instanceof Error ? error.message : String(error);
    return {
      status: {
        level: 'error',
        message: `Map initialization failed: ${message}`,
      },
      mapResult: null,
    };
  }
}

export async function loadMapStatus(
  mapPreviewService: Pick<MapPreviewService, 'load'>,
  container: HTMLDivElement,
  pmtilesHttpUrl: string,
): Promise<UiStatus> {
  const result = await loadMapWithStatus(mapPreviewService, container, pmtilesHttpUrl);
  return result.status;
}

function mapLoadResultToStatus(result: MapLoadResult): UiStatus {
  const skippedOverlayText =
    result.skippedOverlayUrls.length > 0
      ? ` Skipped ${result.skippedOverlayUrls.length} invalid PMTiles overlay(s).`
      : '';

  return {
    level: 'success',
    message: `Loaded ${result.sourceUrl}. Rendered ${result.availableLayers.length} base PMTiles source layers, ${result.overlayUrls.length} PMTiles overlay(s), and ${result.geoJsonUrls.length} GeoJSON overlay(s).${skippedOverlayText}`,
  };
}
