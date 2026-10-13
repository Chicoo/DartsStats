import { useEffect, useRef, useState } from 'react';
import maplibregl from 'maplibre-gl';
import { PMTiles, Protocol } from 'pmtiles';
import { layers, namedFlavor } from '@protomaps/basemaps';
import type { VenueInfo } from '../types';
import 'maplibre-gl/dist/maplibre-gl.css';
import './VenueMap.css';

const protocol = new Protocol();
maplibregl.addProtocol('pmtiles', protocol.tile);
const archive = new PMTiles(`${window.location.origin}/protomaps/main.pmtiles`);
protocol.add(archive);

interface VenueMapProps {
  venue: VenueInfo;
  isExpanded: boolean;
}

export function VenueMap({ venue, isExpanded }: VenueMapProps) {
  const container = useRef<HTMLDivElement>(null);
  const mapRef = useRef<maplibregl.Map | null>(null);
  const [status, setStatus] = useState('Loading venue map…');
  const [failed, setFailed] = useState(false);
  const { latitude, longitude, name } = venue;

  useEffect(() => {
    let disposed = false;
    let cancelled = false;
    let map: maplibregl.Map | undefined;
    let observer: ResizeObserver | undefined;
    const unavailable = (message = 'Map unavailable. Please try again later.') => {
      if (disposed) return;
      cancelled = true;
      setStatus(message);
      setFailed(true);
      observer?.disconnect();
      clearTimeout(timeout);
      map?.remove();
      map = undefined;
      mapRef.current = null;
    };

    const load = async () => {
      if (latitude == null || longitude == null || !Number.isFinite(latitude)
        || !Number.isFinite(longitude) || Math.abs(latitude) > 90 || Math.abs(longitude) > 180) {
        unavailable('Venue location unavailable.');
        return;
      }
      const probe = document.createElement('canvas');
      const gl = probe.getContext('webgl2');
      if (!gl) {
        unavailable('Map unavailable in this browser.');
        return;
      }
      gl.getExtension('WEBGL_lose_context')?.loseContext();
      try {
        const header = await archive.getHeader();
        if (disposed || cancelled || !container.current) return;
        const longitudeCovered = header.minLon <= header.maxLon
          ? longitude >= header.minLon && longitude <= header.maxLon
          : longitude >= header.minLon || longitude <= header.maxLon;
        if (!longitudeCovered || latitude < header.minLat || latitude > header.maxLat) {
          unavailable('Map data does not cover this venue.');
          return;
        }
        map = new maplibregl.Map({
          container: container.current,
          center: [longitude, latitude],
          zoom: Math.max(header.minZoom, Math.min(14, header.maxZoom)),
          minZoom: header.minZoom,
          maxZoom: header.maxZoom,
          scrollZoom: false,
          style: {
            version: 8,
            glyphs: `${window.location.origin}/protomaps/fonts/{fontstack}/{range}.pbf`,
            sprite: `${window.location.origin}/protomaps/sprites/v4/light`,
            sources: {
              basemap: {
                type: 'vector',
                url: `pmtiles://${archive.source.getKey()}`,
                attribution: '<a href="https://protomaps.com">Protomaps</a> | © <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>',
              },
            },
            layers: layers('basemap', namedFlavor('light'), { lang: 'en' }),
          },
        });
        mapRef.current = map;
        map.addControl(new maplibregl.NavigationControl({ showCompass: false }), 'top-right');
        const pin = document.createElement('div');
        pin.className = 'venue-map-pin';
        const label = document.createElement('span');
        label.textContent = name;
        const dot = document.createElement('span');
        dot.className = 'venue-map-pin-dot';
        pin.append(label, dot);
        new maplibregl.Marker({ element: pin, anchor: 'bottom' })
          .setLngLat([longitude, latitude]).addTo(map);
        map.on('error', () => unavailable());
        map.once('load', () => {
          if (!disposed) setStatus('');
          clearTimeout(timeout);
        });
        observer = new ResizeObserver(() => map?.resize());
        observer.observe(container.current);
      } catch {
        unavailable();
      }
    };
    setStatus('Loading venue map…');
    setFailed(false);
    const timeout = setTimeout(() => unavailable(), 20000);
    void load();
    return () => {
      disposed = true;
      clearTimeout(timeout);
      observer?.disconnect();
      map?.remove();
      mapRef.current = null;
    };
  }, [latitude, longitude, name]);

  useEffect(() => {
    if (isExpanded) mapRef.current?.resize();
  }, [isExpanded]);

  return (
    <section className="venue-map-section" aria-label={`${name} location`}>
      <h5>Venue Location</h5>
      <div className="venue-map-frame">
        <div ref={container} className="venue-map" aria-label={`Map showing ${name}`} hidden={failed} />
        {status && <p className="venue-map-status" role="status">{status}</p>}
      </div>
    </section>
  );
}
