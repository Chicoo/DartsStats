import { useEffect } from 'react';
import './Login.css';

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5167';

export function Login() {
  useEffect(() => {
    // Automatically redirect to backend auth endpoint
    const returnUrl = encodeURIComponent(window.location.origin + '/auth/callback');
    window.location.href = `${API_BASE_URL}/api/auth/login?returnUrl=${returnUrl}`;
  }, []);

  return (
    <div className="login-container">
      <div className="login-box">
        <h1>🎯 Redirecting to Login...</h1>
        <p>Please wait while we redirect you to the authentication page.</p>
      </div>
    </div>
  );
}
