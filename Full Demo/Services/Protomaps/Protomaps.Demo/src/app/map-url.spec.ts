import { describe, expect, it } from 'vitest';
import { resolvePmtilesHttpUrl, toPmtilesProtocolUrl } from './map-url';

describe('resolvePmtilesHttpUrl', () => {
  it('uses query value when present and valid', () => {
    const result = resolvePmtilesHttpUrl(
      '?pmtilesUrl=https%3A%2F%2Fcdn.example%2Fmaps.pmtiles',
      'http://stored.example/main.pmtiles',
      'http://fallback/main.pmtiles',
    );

    expect(result).toBe('https://cdn.example/maps.pmtiles');
  });

  it('uses stored value when query is missing', () => {
    const result = resolvePmtilesHttpUrl(
      '',
      'http://stored.example/main.pmtiles',
      'http://fallback/main.pmtiles',
    );

    expect(result).toBe('http://stored.example/main.pmtiles');
  });

  it('uses fallback when query and stored values are invalid', () => {
    const result = resolvePmtilesHttpUrl(
      '?pmtilesUrl=file%3A%2F%2Fnot-allowed',
      'not-a-url',
      'http://fallback/main.pmtiles',
    );

    expect(result).toBe('http://fallback/main.pmtiles');
  });
});

describe('toPmtilesProtocolUrl', () => {
  it('prefixes http URLs with pmtiles://', () => {
    expect(toPmtilesProtocolUrl('http://localhost:5138/protomaps/main.pmtiles')).toBe(
      'pmtiles://http://localhost:5138/protomaps/main.pmtiles',
    );
  });

  it('keeps pmtiles:// URLs unchanged', () => {
    expect(toPmtilesProtocolUrl('pmtiles://https://example/maps.pmtiles')).toBe(
      'pmtiles://https://example/maps.pmtiles',
    );
  });

  it('throws for non-http protocols', () => {
    expect(() => toPmtilesProtocolUrl('file:///tmp/map.pmtiles')).toThrowError(
      /absolute http\(s\) URL/i,
    );
  });
});
