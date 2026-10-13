import { describe, expect, it } from 'vitest';
import {
  DEFAULT_GLYPHS_HTTP_URL,
  DEFAULT_PM_TILES_HTTP_URL,
  DEFAULT_SPRITE_HTTP_URL,
} from './map-url.constants';

describe('map-url constants', () => {
  it('uses local default glyph and sprite endpoints', () => {
    expect(DEFAULT_PM_TILES_HTTP_URL).toBe('http://localhost:5138/protomaps/main.pmtiles');
    expect(DEFAULT_GLYPHS_HTTP_URL).toBe(
      'http://localhost:5138/protomaps/fonts/{fontstack}/{range}.pbf',
    );
    expect(DEFAULT_SPRITE_HTTP_URL).toBe('http://localhost:5138/protomaps/sprites/v4/light');
  });
});
