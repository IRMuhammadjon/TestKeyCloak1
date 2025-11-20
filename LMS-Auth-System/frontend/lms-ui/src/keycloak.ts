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
  const tokenUrl = `${keycloak.authServerUrl}/realms/${keycloak.realm}/protocol/openid-connect/token`;

  const params = new URLSearchParams();
  params.append('client_id', keycloak.clientId!);
  params.append('grant_type', 'password');
  params.append('username', username);
  params.append('password', password);

  try {
    const response = await axios.post(tokenUrl, params, {
      headers: {
        'Content-Type': 'application/x-www-form-urlencoded',
      },
    });

    const { access_token, refresh_token } = response.data;

    // Manually set tokens in keycloak instance
    keycloak.token = access_token;
    keycloak.refreshToken = refresh_token;
    keycloak.authenticated = true;

    // Parse token
    keycloak.tokenParsed = JSON.parse(atob(access_token.split('.')[1]));

    // Store tokens in session storage
    sessionStorage.setItem('kc_token', access_token);
    sessionStorage.setItem('kc_refreshToken', refresh_token);

  } catch (error: any) {
    console.error('Login failed:', error.response?.data || error.message);
    throw new Error(error.response?.data?.error_description || 'Login failed. Please check your credentials.');
  }
};

export default keycloak;
