// @ts-ignore
import Keycloak from 'keycloak-js';

const keycloak = new Keycloak({
  url: process.env.REACT_APP_KEYCLOAK_URL || 'http://localhost:8080',
  realm: process.env.REACT_APP_KEYCLOAK_REALM || 'lms-realm',
  clientId: process.env.REACT_APP_KEYCLOAK_CLIENT_ID || 'lms-frontend',
});

export default keycloak;
