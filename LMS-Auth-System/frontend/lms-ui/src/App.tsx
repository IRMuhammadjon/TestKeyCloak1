import React, { useEffect, useState } from 'react';
import keycloak, { loginWithCredentials } from './keycloak';
import AdminPanel from './components/AdminPanel';
import LoginPage from './components/LoginPage';
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
  const [loginLoading, setLoginLoading] = useState(false);
  const [loginError, setLoginError] = useState<string | null>(null);
  const [showAdminPanel, setShowAdminPanel] = useState(false);

  useEffect(() => {
    // Check if user is already logged in (has token in session)
    const savedToken = sessionStorage.getItem('kc_token');
    const savedRefreshToken = sessionStorage.getItem('kc_refreshToken');

    if (savedToken && savedRefreshToken) {
      keycloak.token = savedToken;
      keycloak.refreshToken = savedRefreshToken;
      keycloak.authenticated = true;

      try {
        keycloak.tokenParsed = JSON.parse(atob(savedToken.split('.')[1]));
        parseAndSetUserInfo(keycloak.tokenParsed);
        setAuthenticated(true);
      } catch (error) {
        console.error('Failed to parse saved token:', error);
        sessionStorage.clear();
      }
    }

    setLoading(false);

    // Token refresh every 30 seconds
    const interval = setInterval(() => {
      if (keycloak.authenticated && keycloak.token) {
        keycloak.updateToken(30).catch(() => {
          console.error('Failed to refresh token');
          handleLogout();
        });
      }
    }, 30000);

    return () => clearInterval(interval);
  }, []);

  const parseAndSetUserInfo = (token: any) => {
    console.log('Keycloak Token:', token);
    console.log('Realm Access:', token.realm_access);
    console.log('Resource Access:', token.resource_access);

    // Get roles from realm_access or resource_access
    let roles: string[] = [];
    if (token.realm_access?.roles) {
      roles = token.realm_access.roles;
    } else if (token.resource_access) {
      Object.values(token.resource_access).forEach((resource: any) => {
        if (resource.roles) {
          roles = [...roles, ...resource.roles];
        }
      });
    }

    console.log('All roles before filter:', roles);

    // Filter out default Keycloak internal roles
    roles = roles.filter(role => {
      const isKeycloakInternal =
        role.startsWith('default-roles-') ||
        role === 'offline_access' ||
        role === 'uma_authorization';

      console.log(`Role "${role}": ${isKeycloakInternal ? 'FILTERED OUT' : 'KEPT'}`);
      return !isKeycloakInternal;
    });

    console.log('Filtered roles:', roles);

    setUserInfo({
      username: token.preferred_username || '',
      email: token.email || '',
      firstName: token.first_name || token.given_name || token.name?.split(' ')[0] || '',
      lastName: token.last_name || token.family_name || token.name?.split(' ')[1] || '',
      roles: roles
    });

    console.log('Parsed User Info:', {
      username: token.preferred_username,
      roles: roles
    });
  };

  const handleLogin = async (username: string, password: string) => {
    setLoginLoading(true);
    setLoginError(null);

    try {
      await loginWithCredentials(username, password);

      if (keycloak.tokenParsed) {
        parseAndSetUserInfo(keycloak.tokenParsed);
        setAuthenticated(true);
      }
    } catch (error: any) {
      setLoginError(error.message || 'Login failed');
    } finally {
      setLoginLoading(false);
    }
  };

  const handleLogout = () => {
    keycloak.authenticated = false;
    keycloak.token = undefined;
    keycloak.refreshToken = undefined;
    keycloak.tokenParsed = undefined;
    sessionStorage.clear();
    setAuthenticated(false);
    setUserInfo(null);
    setShowAdminPanel(false);
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
      <LoginPage
        onLogin={handleLogin}
        loading={loginLoading}
        error={loginError}
      />
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
              <button onClick={handleLogout} className="btn-nav btn-logout">
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
            <button onClick={handleLogout} className="btn-logout-main">
              Logout
            </button>
          </div>
        </div>
      </header>
    </div>
  );
}

export default App;
