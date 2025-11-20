import React, { useState } from 'react';
import './LoginPage.css';

interface LoginPageProps {
  onLogin: (username: string, password: string) => Promise<void>;
  loading: boolean;
  error: string | null;
}

const LoginPage: React.FC<LoginPageProps> = ({ onLogin, loading, error }) => {
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    await onLogin(username, password);
  };

  return (
    <div className="login-page">
      <div className="login-container">
        <div className="login-header">
          <h1>LMS System</h1>
          <p>Learning Management System</p>
        </div>

        <form onSubmit={handleSubmit} className="login-form">
          <h2>Sign In</h2>

          {error && (
            <div className="error-message">
              {error}
            </div>
          )}

          <div className="form-group">
            <label htmlFor="username">Username or Email</label>
            <input
              type="text"
              id="username"
              value={username}
              onChange={(e) => setUsername(e.target.value)}
              placeholder="Enter your username"
              required
              autoFocus
              disabled={loading}
            />
          </div>

          <div className="form-group">
            <label htmlFor="password">Password</label>
            <input
              type="password"
              id="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              placeholder="Enter your password"
              required
              disabled={loading}
            />
          </div>

          <button type="submit" className="login-button" disabled={loading}>
            {loading ? 'Signing in...' : 'Sign In'}
          </button>

          <div className="login-footer">
            <p className="demo-credentials">
              <strong>Demo Credentials:</strong><br />
              Admin: admin / admin123<br />
              User: testuser / user123
            </p>
          </div>
        </form>
      </div>
    </div>
  );
};

export default LoginPage;
