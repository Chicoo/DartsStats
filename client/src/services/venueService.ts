import type { VenueInfo } from '../types';

// Mapping of Premier League Darts nights to venue information
const venueMapping: Record<string, { city: string; venue: string }> = {
  'Night 01': { city: 'Belfast', venue: 'SSE Arena Belfast' },
  'Night 02': { city: 'Glasgow', venue: 'P&J Live' },
  'Night 03': { city: 'Dublin', venue: '3Arena' },
  'Night 04': { city: 'Exeter', venue: 'Westpoint Arena' },
  'Night 05': { city: 'Brighton', venue: 'Brighton Centre' },
  'Night 06': { city: 'Nottingham', venue: 'Motorpoint Arena Nottingham' },
  'Night 07': { city: 'Cardiff', venue: 'Motorpoint Arena Cardiff' },
  'Night 08': { city: 'Newcastle', venue: 'Utilita Arena Newcastle' },
  'Night 09': { city: 'Berlin', venue: 'Uber Arena' },
  'Night 10': { city: 'Manchester', venue: 'AO Arena' },
  'Night 11': { city: 'Rotterdam', venue: 'Rotterdam Ahoy' },
  'Night 12': { city: 'Liverpool', venue: 'M&S Bank Arena' },
  'Night 13': { city: 'Birmingham', venue: 'Resorts World Arena' },
  'Night 14': { city: 'Leeds', venue: 'First Direct Arena' },
  'Night 15': { city: 'Aberdeen', venue: 'P&J Live' },
  'Night 16': { city: 'Sheffield', venue: 'Utilita Arena Sheffield' },
  'Semi-Final 1': { city: 'London', venue: 'The O2 Arena' },
  'Semi-Final 2': { city: 'London', venue: 'The O2 Arena' },
  'Final': { city: 'London', venue: 'The O2 Arena' },
};

// Function to extract venue info from Wikipedia API response
const parseWikipediaResponse = (data: Record<string, unknown>, venueName: string, city: string): VenueInfo => {
  const query = data.query as Record<string, unknown> | undefined;
  const pages = query?.pages as Record<string, unknown> | undefined;
  if (!pages) {
    return createFallbackVenueInfo(venueName, city);
  }

  const pageId = Object.keys(pages)[0];
  const page = pages[pageId] as Record<string, unknown>;
  
  if (!page || page.missing) {
    return createFallbackVenueInfo(venueName, city);
  }

  const extract = (page.extract as string) || '';
  const title = (page.title as string) || venueName;
  
  // Extract capacity from the text (common patterns)
  const capacityMatch = extract.match(/capacity[:\s]+(?:of\s+)?([0-9,]+)/i) || 
                       extract.match(/seats?\s+([0-9,]+)/i) ||
                       extract.match(/([0-9,]+)\s+(?:capacity|seats?)/i);
  
  const capacity = capacityMatch ? capacityMatch[1] : 'Unknown';
  
  // Extract opening year
  const openedMatch = extract.match(/opened?\s+(?:in\s+)?(\d{4})/i) ||
                     extract.match(/built\s+(?:in\s+)?(\d{4})/i);
  
  const opened = openedMatch ? openedMatch[1] : undefined;
  
  // Get thumbnail image if available
  const thumbnail = page.thumbnail as Record<string, unknown> | undefined;
  const thumbnailSource = thumbnail?.source as string | undefined;
  
  return {
    name: title,
    city: city,
    capacity: capacity,
    description: extract.substring(0, 500) + (extract.length > 500 ? '...' : ''),
    image: thumbnailSource,
    opened: opened,
  };
};

const createFallbackVenueInfo = (venueName: string, city: string): VenueInfo => ({
  name: venueName,
  city: city,
  capacity: 'Information not available',
  description: `${venueName} is a major entertainment venue located in ${city}. This venue regularly hosts Premier League Darts and other major sporting events.`,
});

export const fetchVenueInfo = async (round: string): Promise<VenueInfo | null> => {
  const venueData = venueMapping[round];
  
  if (!venueData) {
    return null;
  }

  const { city, venue } = venueData;
  
  try {
    // Use Wikipedia API to get venue information
    const wikipediaUrl = `https://en.wikipedia.org/api/rest_v1/page/summary/${encodeURIComponent(venue)}`;
    
    const response = await fetch(wikipediaUrl, {
      headers: {
        'Accept': 'application/json',
        'User-Agent': 'DartsStats/1.0 (Educational Project)'
      }
    });

    if (!response.ok) {
      throw new Error(`Wikipedia API error: ${response.status}`);
    }

    const data = await response.json();
    
    return {
      name: data.title || venue,
      city: city,
      capacity: data.description || 'Information not available',
      description: data.extract || `${venue} is a major entertainment venue located in ${city}.`,
      image: data.thumbnail?.source,
      website: data.content_urls?.desktop?.page,
    };
    
  } catch (error) {
    console.warn('Failed to fetch venue info from Wikipedia:', error);
    
    // Fallback: try alternative Wikipedia API
    try {
      const fallbackUrl = `https://en.wikipedia.org/w/api.php?action=query&format=json&prop=extracts|pageimages&exintro=1&explaintext=1&piprop=thumbnail&pithumbsize=300&titles=${encodeURIComponent(venue)}&origin=*`;
      
      const fallbackResponse = await fetch(fallbackUrl);
      
      if (!fallbackResponse.ok) {
        throw new Error(`Fallback API error: ${fallbackResponse.status}`);
      }
      
      const fallbackData = await fallbackResponse.json();
      return parseWikipediaResponse(fallbackData, venue, city);
      
    } catch (fallbackError) {
      console.warn('Fallback venue info fetch failed:', fallbackError);
      return createFallbackVenueInfo(venue, city);
    }
  }
};

export const getVenueForRound = (round: string): { city: string; venue: string } | null => {
  return venueMapping[round] || null;
};
