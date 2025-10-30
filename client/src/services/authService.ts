import { UserManager, User } from 'oidc-client-ts';
import type { UserManagerSettings } from 'oidc-client-ts';

class AuthService {
    private userManager: UserManager;
    private user: User | null = null;

    constructor() {
        const settings: UserManagerSettings = {
            authority: import.meta.env.VITE_KEYCLOAK_AUTHORITY || 'https://keycloak.example.com/realms/dartsstats',
            client_id: import.meta.env.VITE_KEYCLOAK_CLIENT_ID || 'dartsstats-web',
            redirect_uri: `${window.location.origin}/callback`,
            post_logout_redirect_uri: window.location.origin,
            response_type: 'code',
            scope: 'openid profile email',
            automaticSilentRenew: true,
            loadUserInfo: true
        };

        this.userManager = new UserManager(settings);
        this.initUser();
    }

    private async initUser() {
        try {
            this.user = await this.userManager.getUser();
        } catch (error) {
            console.error('Error loading user:', error);
        }
    }

    async login(): Promise<void> {
        await this.userManager.signinRedirect();
    }

    async handleCallback(): Promise<User | null> {
        try {
            const user = await this.userManager.signinRedirectCallback();
            this.user = user;
            return user;
        } catch (error) {
            console.error('Error handling callback:', error);
            return null;
        }
    }

    async logout(): Promise<void> {
        await this.userManager.signoutRedirect();
        this.user = null;
    }

    async getUser(): Promise<User | null> {
        if (!this.user) {
            this.user = await this.userManager.getUser();
        }
        return this.user;
    }

    async getAccessToken(): Promise<string | null> {
        const user = await this.getUser();
        return user?.access_token || null;
    }

    async isAuthenticated(): Promise<boolean> {
        const user = await this.getUser();
        return !!user && !user.expired;
    }

    async isAdmin(): Promise<boolean> {
        const user = await this.getUser();
        if (!user) return false;

        // Check if user has admin role in Keycloak token
        const profile = user.profile as Record<string, unknown>;
        const realmAccess = profile?.realm_access as Record<string, unknown> | undefined;
        const roles = realmAccess?.roles as string[] || [];
        return roles.includes('admin');
    }
}

export const authService = new AuthService();
export default authService;
