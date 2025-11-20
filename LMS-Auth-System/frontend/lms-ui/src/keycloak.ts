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

export default keycloak;
