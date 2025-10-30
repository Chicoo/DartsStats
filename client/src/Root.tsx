import { BrowserRouter, Routes, Route, Link } from 'react-router-dom';
import { useState, useEffect } from 'react';
import App from './App';
import { Management } from './components/Management';
import { AuthCallback } from './components/AuthCallback';
import { ProtectedRoute } from './components/ProtectedRoute';
import authService from './services/authService';
import './Root.css';

function Root() {
  const [isAuthenticated, setIsAuthenticated] = useState(false);
  const [isAdmin, setIsAdmin] = useState(false);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    checkAuth();
  }, []);

  const checkAuth = async () => {
    try {
      const authenticated = await authService.isAuthenticated();
      setIsAuthenticated(authenticated);
      
      if (authenticated) {
        const admin = await authService.isAdmin();
        setIsAdmin(admin);
      }
    } catch (error) {
      console.error('Auth check error:', error);
    } finally {
      setLoading(false);
    }
  };

  const handleLogin = async () => {
    await authService.login();
  };

  const handleLogout = async () => {
    await authService.logout();
    setIsAuthenticated(false);
    setIsAdmin(false);
  };

  return (
    <BrowserRouter>
      <div className="app-root">
        <nav className="app-nav">
          <div className="nav-container">
            <div className="nav-brand">
              <Link to="/">🎯 DartsStats</Link>
            </div>
            <div className="nav-links">
              <Link to="/" className="nav-link">Home</Link>
              {isAdmin && (
                <Link to="/management" className="nav-link">🔐 Management</Link>
              )}
              <div className="nav-auth">
                {loading ? (
                  <span className="nav-loading">...</span>
                ) : isAuthenticated ? (
                  <button onClick={handleLogout} className="btn-auth btn-logout">
                    🚪 Logout
                  </button>
                ) : (
                  <button onClick={handleLogin} className="btn-auth btn-login">
                    🔑 Admin Login
                  </button>
                )}
              </div>
            </div>
          </div>
        </nav>
        
        <main className="app-main">
          <Routes>
            <Route path="/" element={<App />} />
            <Route path="/callback" element={<AuthCallback />} />
            <Route 
              path="/management" 
              element={
                <ProtectedRoute requireAdmin={true}>
                  <Management />
                </ProtectedRoute>
              } 
            />
          </Routes>
        </main>
      </div>
    </BrowserRouter>
  );
}

export default Root;
