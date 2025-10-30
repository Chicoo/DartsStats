// Use Aspire service discovery or fallback to localhost for development
const getApiBaseUrl = () => {
    // Check for Aspire-provided API base URL
    const aspireApiUrl = import.meta.env.VITE_API_BASE_URL;
    if (aspireApiUrl) {
        return `${aspireApiUrl}/api`;
    }
    
    // Final fallback for local development
    return 'http://localhost:5167/api';
};

const API_BASE_URL = getApiBaseUrl();

export const fetchPlayers = async () => {
    const response = await fetch(`${API_BASE_URL}/players`);
    if (!response.ok) {
        throw new Error('Failed to fetch players');
    }
    return response.json();
};

export const fetchMatches = async (season?: string, round?: string) => {
    const params = new URLSearchParams();
    if (season) params.append('season', season);
    if (round) params.append('round', round);
    
    const url = params.toString() 
        ? `${API_BASE_URL}/matches?${params.toString()}`
        : `${API_BASE_URL}/matches`;
        
    const response = await fetch(url);
    if (!response.ok) {
        throw new Error('Failed to fetch matches');
    }
    return response.json();
};

export const fetchRounds = async (season?: string) => {
    const url = season 
        ? `${API_BASE_URL}/matches/rounds?season=${season}`
        : `${API_BASE_URL}/matches/rounds`;
    const response = await fetch(url);
    if (!response.ok) {
        throw new Error('Failed to fetch rounds');
    }
    return response.json();
};

export const fetchVenueInfo = async (round: string) => {
    const response = await fetch(`${API_BASE_URL}/venues/${encodeURIComponent(round)}`);
    if (!response.ok) {
        if (response.status === 404) {
            throw new Error(`No venue information found for ${round}`);
        }
        throw new Error(`Failed to fetch venue information: ${response.status}`);
    }
    return response.json();
};

// Management API endpoints (require authentication)
export const updateMatch = async (id: number, matchData: any, token: string) => {
    const response = await fetch(`${API_BASE_URL}/management/matches/${id}`, {
        method: 'PUT',
        headers: {
            'Content-Type': 'application/json',
            'Authorization': `Bearer ${token}`
        },
        body: JSON.stringify(matchData)
    });
    
    if (!response.ok) {
        const error = await response.json().catch(() => ({ message: 'Failed to update match' }));
        throw new Error(error.message || 'Failed to update match');
    }
    
    return response.json();
};

export const getMatchForEdit = async (id: number, token: string) => {
    const response = await fetch(`${API_BASE_URL}/management/matches/${id}`, {
        headers: {
            'Authorization': `Bearer ${token}`
        }
    });
    
    if (!response.ok) {
        throw new Error('Failed to fetch match');
    }
    
    return response.json();
};

export const deleteMatch = async (id: number, token: string) => {
    const response = await fetch(`${API_BASE_URL}/management/matches/${id}`, {
        method: 'DELETE',
        headers: {
            'Authorization': `Bearer ${token}`
        }
    });
    
    if (!response.ok) {
        const error = await response.json().catch(() => ({ message: 'Failed to delete match' }));
        throw new Error(error.message || 'Failed to delete match');
    }
};
