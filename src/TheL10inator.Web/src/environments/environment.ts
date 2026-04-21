/**
 * Production environment. useDevBypass must be false here so a production build never
 * carries the X-Dev-User-Email code path — see standards/environments.md §Default-OFF.
 */
export const environment = {
  production: true,
  useDevBypass: false,
  msal: {
    clientId: 'REPLACE_ME',
    authority: 'https://login.microsoftonline.com/REPLACE_ME',
    redirectUri: '/login',
    apiScopes: ['api://REPLACE_ME/.default'],
  },
  apiBaseUrl: '',
};
