import { describe, expect, it } from 'vitest';
import { DEFAULT_GLYPHS_HTTP_URL, DEFAULT_SPRITE_HTTP_URL } from './map-url.constants';
import { createStyle } from './map-preview.service';

describe('createStyle', () => {
  it('includes local glyph and sprite endpoints', () => {
    const style = createStyle(
      {
        id: 'protomaps',
        url: 'http://localhost:5138/protomaps/main.pmtiles',
        sourceLayers: ['roads', 'places'],
        isOverlay: false,
      },
      [],
      [],
    );

    expect(style.glyphs).toBe(DEFAULT_GLYPHS_HTTP_URL);
    expect(style.sprite).toBe(DEFAULT_SPRITE_HTTP_URL);
    expect(style.sources.protomaps).toMatchObject({
      type: 'vector',
      url: 'pmtiles://http://localhost:5138/protomaps/main.pmtiles',
    });
    expect(style.layers.length).toBeGreaterThan(0);
  });
});
