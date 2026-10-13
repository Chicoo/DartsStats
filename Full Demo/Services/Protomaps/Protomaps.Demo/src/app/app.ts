import { CommonModule } from '@angular/common';
import { AfterViewInit, Component, ElementRef, OnDestroy, ViewChild, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  DEFAULT_GEOJSON_HTTP_URLS,
  DEFAULT_GLYPHS_HTTP_URL,
  DEFAULT_OVERLAY_PM_TILES_HTTP_URLS,
  DEFAULT_PM_TILES_HTTP_URL,
  DEFAULT_SPRITE_HTTP_URL,
  PMTILES_URL_STORAGE_KEY,
} from './map-url.constants';
import { loadMapWithStatus, type UiStatus } from './map-loader';
import { MapPreviewService } from './map-preview.service';
import { resolvePmtilesHttpUrl } from './map-url';

@Component({
  selector: 'app-root',
  imports: [CommonModule, FormsModule],
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App implements AfterViewInit, OnDestroy {
  @ViewChild('mapContainer') private readonly mapContainer?: ElementRef<HTMLDivElement>;

  protected readonly title = signal('Protomaps Host Demo');
  protected readonly defaultPmtilesUrl = DEFAULT_PM_TILES_HTTP_URL;
  protected readonly overlayPmtilesUrls = DEFAULT_OVERLAY_PM_TILES_HTTP_URLS;
  protected readonly geoJsonUrls = DEFAULT_GEOJSON_HTTP_URLS;
  protected readonly glyphsUrl = DEFAULT_GLYPHS_HTTP_URL;
  protected readonly spriteUrl = DEFAULT_SPRITE_HTTP_URL;
  protected readonly status = signal<UiStatus>({
    level: 'info',
    message: 'Set the PMTiles URL and load the map.',
  });
  protected readonly isLoading = signal(false);

  protected pmtilesUrl = resolvePmtilesHttpUrl(
    window.location.search,
    window.localStorage.getItem(PMTILES_URL_STORAGE_KEY),
    DEFAULT_PM_TILES_HTTP_URL,
  );

  public constructor(private readonly mapPreviewService: MapPreviewService) {}

  public async ngAfterViewInit(): Promise<void> {
    await this.loadMap();
  }

  public ngOnDestroy(): void {
    this.mapPreviewService.destroy();
  }

  protected async onLoadMap(event: Event): Promise<void> {
    event.preventDefault();
    await this.loadMap();
  }

  private async loadMap(): Promise<void> {
    const container = this.mapContainer?.nativeElement;
    if (!container) {
      this.status.set({
        level: 'error',
        message: 'Map container was not initialized.',
      });
      return;
    }

    this.isLoading.set(true);

    const loadResult = await loadMapWithStatus(this.mapPreviewService, container, this.pmtilesUrl);
    this.status.set(loadResult.status);

    if (loadResult.status.level !== 'error') {
      window.localStorage.setItem(PMTILES_URL_STORAGE_KEY, this.pmtilesUrl.trim());
    }

    this.isLoading.set(false);
  }
}
