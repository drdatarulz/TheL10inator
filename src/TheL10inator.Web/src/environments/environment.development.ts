/**
 * Development environment. Dev-bypass is enabled so local docker-compose stacks can
 * authenticate via the X-Dev-User-Email header instead of a real Entra tenant.
 */
export const environment = {
  production: false,
  useDevBypass: true,
  msal: {
    clientId: 'dev-placeholder',
    authority: 'https://login.microsoftonline.com/common',
    redirectUri: '/login',
    apiScopes: [],
  },
  apiBaseUrl: '',
};
