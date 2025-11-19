import Keycloak from 'keycloak-js';

const keycloak = new Keycloak({
  url: 'http://localhost:8080',
  realm: 'lms-realm',
  clientId: 'lms-frontend',
});

export default keycloak;
