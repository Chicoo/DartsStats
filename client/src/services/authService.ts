const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5167';

interface LoginResponse {
    token: string;
    refreshToken: string;
    username: string;
    isAdmin: boolean;
    expiresIn: number;
}

class AuthService {
    private token: string | null = null;
    private refreshToken: string | null = null;
    private username: string | null = null;
    private isAdminUser: boolean = false;

    constructor() {
        this.loadFromStorage();
    }

    private loadFromStorage() {
        this.token = localStorage.getItem('auth_token');
        this.refreshToken = localStorage.getItem('auth_refresh_token');
        this.username = localStorage.getItem('auth_username');
        this.isAdminUser = localStorage.getItem('auth_isAdmin') === 'true';
    }

    private saveToStorage(token: string, refreshToken: string, username: string, isAdmin: boolean) {
        localStorage.setItem('auth_token', token);
        localStorage.setItem('auth_refresh_token', refreshToken);
        localStorage.setItem('auth_username', username);
        localStorage.setItem('auth_isAdmin', isAdmin.toString());
        this.token = token;
        this.refreshToken = refreshToken;
        this.username = username;
        this.isAdminUser = isAdmin;
    }

    // Public method to save auth data from callback
    saveAuthData(token: string, refreshToken: string, username: string, isAdmin: boolean) {
        this.saveToStorage(token, refreshToken, username, isAdmin);
    }

    private clearStorage() {
        localStorage.removeItem('auth_token');
        localStorage.removeItem('auth_refresh_token');
        localStorage.removeItem('auth_username');
        localStorage.removeItem('auth_isAdmin');
        this.token = null;
        this.refreshToken = null;
        this.username = null;
        this.isAdminUser = false;
    }

    async logout(): Promise<void> {
        if (this.refreshToken) {
            try {
                await fetch(`${API_BASE_URL}/api/auth/logout`, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'Authorization': `Bearer ${this.token}`,
                    },
                    body: JSON.stringify({ refreshToken: this.refreshToken }),
                });
            } catch (error) {
                console.error('Logout request failed:', error);
            }
        }
        this.clearStorage();
    }

    async getAccessToken(): Promise<string | null> {
        // Check if token is expired and refresh if needed
        if (this.token && await this.isTokenExpired()) {
            await this.refreshAccessToken();
        }
        return this.token;
    }

    private async isTokenExpired(): Promise<boolean> {
        if (!this.token) return true;
        
        try {
            const payload = JSON.parse(atob(this.token.split('.')[1]));
            const exp = payload.exp * 1000; // Convert to milliseconds
            // Refresh if token expires in less than 5 minutes
            return Date.now() > (exp - 5 * 60 * 1000);
        } catch {
            return true;
        }
    }

    private async refreshAccessToken(): Promise<void> {
        if (!this.refreshToken) {
            this.clearStorage();
            throw new Error('No refresh token available');
        }

        try {
            const response = await fetch(`${API_BASE_URL}/api/auth/refresh`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({ refreshToken: this.refreshToken }),
            });

            if (!response.ok) {
                this.clearStorage();
                throw new Error('Token refresh failed');
            }

            const data: LoginResponse = await response.json();
            this.saveToStorage(data.token, data.refreshToken, data.username, data.isAdmin);
        } catch (error) {
            this.clearStorage();
            throw error;
        }
    }

    async isAuthenticated(): Promise<boolean> {
        if (!this.token) return false;
        
        // Try to refresh if expired
        if (await this.isTokenExpired()) {
            try {
                await this.refreshAccessToken();
                return true;
            } catch {
                return false;
            }
        }
        
        return true;
    }

    async isAdmin(): Promise<boolean> {
        return this.isAdminUser && await this.isAuthenticated();
    }

    getUsername(): string | null {
        return this.username;
    }
}

export const authService = new AuthService();
export default authService;
