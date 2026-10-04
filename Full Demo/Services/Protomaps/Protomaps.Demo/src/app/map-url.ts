export function resolvePmtilesHttpUrl(
  queryString: string,
  storedValue: string | null,
  fallback: string,
): string {
  const fromQuery = readQueryParam(queryString, 'pmtilesUrl');
  if (isHttpUrl(fromQuery)) {
    return fromQuery;
  }

  if (isHttpUrl(storedValue)) {
    return storedValue;
  }

  return fallback;
}

export function toPmtilesProtocolUrl(url: string): string {
  const normalized = url.trim();

  if (normalized.startsWith('pmtiles://')) {
    return normalized;
  }

  if (!isHttpUrl(normalized)) {
    throw new Error('PMTiles URL must be an absolute http(s) URL.');
  }

  return `pmtiles://${normalized}`;
}

function readQueryParam(queryString: string, parameterName: string): string | null {
  const value = new URLSearchParams(queryString).get(parameterName);
  return value?.trim() || null;
}

function isHttpUrl(value: string | null): value is string {
  if (!value) {
    return false;
  }

  try {
    const parsed = new URL(value.trim());
    return parsed.protocol === 'http:' || parsed.protocol === 'https:';
  } catch {
    return false;
  }
}
