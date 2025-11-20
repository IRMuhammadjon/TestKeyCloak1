// @ts-ignore
import Keycloak from 'keycloak-js';
import axios from 'axios';

const keycloak = new Keycloak({
  url: process.env.REACT_APP_KEYCLOAK_URL || 'http://localhost:8080',
  realm: process.env.REACT_APP_KEYCLOAK_REALM || 'lms-realm',
  clientId: process.env.REACT_APP_KEYCLOAK_CLIENT_ID || 'lms-frontend',
});

// Custom login function using Resource Owner Password Credentials flow
export const loginWithCredentials = async (username: string, password: string): Promise<void> => {
  // Use direct URLs since keycloak instance may not be initialized yet
  const keycloakUrl = process.env.REACT_APP_KEYCLOAK_URL || 'http://localhost:8080';
  const realmName = process.env.REACT_APP_KEYCLOAK_REALM || 'lms-realm';
  const clientId = process.env.REACT_APP_KEYCLOAK_CLIENT_ID || 'lms-frontend';

  const tokenUrl = `${keycloakUrl}/realms/${realmName}/protocol/openid-connect/token`;

  const params = new URLSearchParams();
  params.append('client_id', clientId);
  params.append('grant_type', 'password');
  params.append('username', username);
  params.append('password', password);

  console.log('Login attempt:', { username, tokenUrl, clientId });

  try {
    const response = await axios.post(tokenUrl, params, {
      headers: {
        'Content-Type': 'application/x-www-form-urlencoded',
      },
    });

    console.log('Login response:', response.data);

    const { access_token, refresh_token } = response.data;

    if (!access_token) {
      throw new Error('No access token received from Keycloak');
    }

    // Manually set tokens in keycloak instance
    keycloak.token = access_token;
    keycloak.refreshToken = refresh_token;
    keycloak.authenticated = true;

    // Parse token
    try {
      const tokenParts = access_token.split('.');
      if (tokenParts.length !== 3) {
        throw new Error('Invalid JWT token format');
      }
      keycloak.tokenParsed = JSON.parse(atob(tokenParts[1]));
      console.log('Token parsed successfully:', keycloak.tokenParsed);
    } catch (parseError) {
      console.error('Token parsing error:', parseError);
      throw new Error('Failed to parse access token');
    }

    // Store tokens in session storage
    sessionStorage.setItem('kc_token', access_token);
    sessionStorage.setItem('kc_refreshToken', refresh_token);

  } catch (error: any) {
    console.error('Login error details:', {
      message: error.message,
      response: error.response?.data,
      status: error.response?.status
    });

    if (error.response?.data?.error_description) {
      throw new Error(error.response.data.error_description);
    } else if (error.response?.data?.error) {
      throw new Error(error.response.data.error);
    } else if (error.message) {
      throw new Error(error.message);
    } else {
      throw new Error('Login failed. Please check your credentials and try again.');
    }
  }
};

// Manual token refresh function
export const refreshToken = async (): Promise<void> => {
  const keycloakUrl = process.env.REACT_APP_KEYCLOAK_URL || 'http://localhost:8080';
  const realmName = process.env.REACT_APP_KEYCLOAK_REALM || 'lms-realm';
  const clientId = process.env.REACT_APP_KEYCLOAK_CLIENT_ID || 'lms-frontend';

  const tokenUrl = `${keycloakUrl}/realms/${realmName}/protocol/openid-connect/token`;

  const currentRefreshToken = sessionStorage.getItem('kc_refreshToken');
  if (!currentRefreshToken) {
    throw new Error('No refresh token available');
  }

  const params = new URLSearchParams();
  params.append('client_id', clientId);
  params.append('grant_type', 'refresh_token');
  params.append('refresh_token', currentRefreshToken);

  try {
    const response = await axios.post(tokenUrl, params, {
      headers: {
        'Content-Type': 'application/x-www-form-urlencoded',
      },
    });

    const { access_token, refresh_token } = response.data;

    if (!access_token) {
      throw new Error('No access token received');
    }

    // Update tokens
    keycloak.token = access_token;
    keycloak.refreshToken = refresh_token;
    keycloak.authenticated = true;

    // Parse token
    const tokenParts = access_token.split('.');
    if (tokenParts.length === 3) {
      keycloak.tokenParsed = JSON.parse(atob(tokenParts[1]));
    }

    // Update session storage
    sessionStorage.setItem('kc_token', access_token);
    sessionStorage.setItem('kc_refreshToken', refresh_token);

    console.log('Token refreshed successfully');
  } catch (error: any) {
    console.error('Token refresh error:', error.response?.data || error.message);
    throw error;
  }
};

export default keycloak;
