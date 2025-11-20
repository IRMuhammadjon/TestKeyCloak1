import React, { useEffect, useState } from 'react';
import keycloak from './keycloak';
import AdminPanel from './components/AdminPanel';
import './App.css';

interface UserInfo {
  username: string;
  email: string;
  firstName: string;
  lastName: string;
  roles: string[];
}

function App() {
  const [authenticated, setAuthenticated] = useState(false);
  const [userInfo, setUserInfo] = useState<UserInfo | null>(null);
  const [loading, setLoading] = useState(true);
  const [showAdminPanel, setShowAdminPanel] = useState(false);

  useEffect(() => {
    keycloak.init({ onLoad: 'login-required', checkLoginIframe: false })
      .then((authenticated: boolean) => {
        setAuthenticated(authenticated);
        if (authenticated && keycloak.tokenParsed) {
          const token = keycloak.tokenParsed as any;
          setUserInfo({
            username: token.preferred_username || '',
            email: token.email || '',
            firstName: token.first_name || token.given_name || '',
            lastName: token.last_name || token.family_name || '',
            roles: token.realm_access?.roles || []
          });
        }
        setLoading(false);
      })
      .catch(() => {
        setLoading(false);
      });

    // Token refresh every 30 seconds
    const interval = setInterval(() => {
      keycloak.updateToken(30).catch(() => {
        console.error('Failed to refresh token');
        keycloak.login();
      });
    }, 30000);

    return () => clearInterval(interval);
  }, []);

  const logout = () => {
    keycloak.logout({ redirectUri: window.location.origin });
  };

  if (loading) {
    return (
      <div className="App">
        <div className="loading-screen">
          <h2>Loading...</h2>
        </div>
      </div>
    );
  }

  if (!authenticated) {
    return (
      <div className="App">
        <div className="error-screen">
          <h2>Not authenticated</h2>
        </div>
      </div>
    );
  }

  const isAdmin = userInfo?.roles.includes('admin');

  if (showAdminPanel && isAdmin) {
    return (
      <div className="App app-admin">
        <nav className="navbar">
          <div className="nav-content">
            <h1>LMS System</h1>
            <div className="nav-right">
              <span className="user-name">{userInfo?.firstName} {userInfo?.lastName}</span>
              <button onClick={() => setShowAdminPanel(false)} className="btn-nav">
                Dashboard
              </button>
              <button onClick={logout} className="btn-nav btn-logout">
                Logout
              </button>
            </div>
          </div>
        </nav>
        <main className="main-content">
          <AdminPanel />
        </main>
      </div>
    );
  }

  return (
    <div className="App">
      <header className="App-header">
        <div className="welcome-container">
          <h1>Welcome to LMS System!</h1>

          <div className="user-card">
            <h2>Hello, {userInfo?.firstName} {userInfo?.lastName}!</h2>
            <div className="user-info">
              <p><strong>Username:</strong> {userInfo?.username}</p>
              <p><strong>Email:</strong> {userInfo?.email}</p>
              <p><strong>Roles:</strong> <span className="roles-badge">{userInfo?.roles.join(', ')}</span></p>
            </div>
          </div>

          {isAdmin && (
            <div className="admin-section">
              <h3>Admin Access</h3>
              <p>You have administrator privileges!</p>
              <button onClick={() => setShowAdminPanel(true)} className="btn-admin-panel">
                Open Admin Panel
              </button>
              <p className="admin-hint">Manage users, assign roles, and more...</p>
            </div>
          )}

          <div className="actions">
            <button onClick={logout} className="btn-logout-main">
              Logout
            </button>
          </div>
        </div>
      </header>
    </div>
  );
}

export default App;
