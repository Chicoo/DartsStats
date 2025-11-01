import type { ReactNode } from 'react';
import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import authService from '../services/authService';

interface ProtectedRouteProps {
  children: ReactNode;
  requireAdmin?: boolean;
}

export function ProtectedRoute({ children, requireAdmin = false }: ProtectedRouteProps) {
  const [isAuthenticated, setIsAuthenticated] = useState<boolean | null>(null);
  const [isAdmin, setIsAdmin] = useState<boolean>(false);
  const [loading, setLoading] = useState(true);
  const navigate = useNavigate();

  useEffect(() => {
    const checkAuth = async () => {
      try {
        const authenticated = await authService.isAuthenticated();
        setIsAuthenticated(authenticated);

        if (authenticated && requireAdmin) {
          const admin = await authService.isAdmin();
          setIsAdmin(admin);
        } else if (authenticated) {
          setIsAdmin(true); // Not checking admin requirement
        }
      } catch (error) {
        console.error('Auth check error:', error);
        setIsAuthenticated(false);
      } finally {
        setLoading(false);
      }
    };

    checkAuth();
  }, [requireAdmin]);

  if (loading) {
    return (
      <div style={{ padding: '2rem', textAlign: 'center' }}>
        <p>🔄 Checking authentication...</p>
      </div>
    );
  }

  if (!isAuthenticated) {
    // Redirect to login
    navigate('/login');
    return null;
  }

  if (requireAdmin && !isAdmin) {
    return (
      <div style={{ padding: '2rem', textAlign: 'center' }}>
        <h2>🔒 Access Denied</h2>
        <p>You don't have administrator privileges to access this section.</p>
        <button onClick={() => navigate('/')}>Return Home</button>
      </div>
    );
  }

  return <>{children}</>;
}
