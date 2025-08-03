export interface Player {
    id: number;
    name: string;
    nickname: string;
    country: string;
}

export interface Match {
    id: number;
    player1Id: number;
    player2Id: number;
    player1?: Player;
    player2?: Player;
    matchDate: string;
    player1Score: number;
    player2Score: number;
    player1Average: number;
    player2Average: number;
    player1180s: number;
    player2180s: number;
    player1HighestCheckout: number;
    player2HighestCheckout: number;
    season: string;
    round: string;
}

export interface VenueInfo {
    name: string;
    city: string;
    capacity: string;
    description: string;
    image?: string;
    website?: string;
    address?: string;
    opened?: string;
    weather?: WeatherInfo;
}

export interface WeatherInfo {
    description?: string;
    temperature?: number;
    humidity?: number;
    windSpeed?: number;
    icon?: string;
    eventDate: string;
}
