import { NextRequest, NextResponse } from 'next/server';
import { getProviders } from '../../_lib';
import { appUrl, callbackUrl, OAUTH_COOKIE, pkceChallenge, randomToken, secureCookies } from '../oauth';

/** Starts "Sign in with Microsoft" (authorization code flow with PKCE, state and nonce). */
export async function GET(request: NextRequest) {
  const providers = await getProviders();
  if (!providers.microsoft.enabled || !providers.microsoft.clientId || !providers.microsoft.authorizeUrl) {
    return NextResponse.redirect(`${appUrl(request)}/auth/login?error=${encodeURIComponent('Microsoft sign in is not enabled.')}`);
  }

  const state = randomToken();
  const nonce = randomToken();
  const verifier = randomToken(48);

  const url = new URL(providers.microsoft.authorizeUrl);
  url.searchParams.set('client_id', providers.microsoft.clientId);
  url.searchParams.set('response_type', 'code');
  url.searchParams.set('response_mode', 'query');
  url.searchParams.set('redirect_uri', callbackUrl(request));
  url.searchParams.set('scope', 'openid profile email');
  url.searchParams.set('state', state);
  url.searchParams.set('nonce', nonce);
  url.searchParams.set('code_challenge', await pkceChallenge(verifier));
  url.searchParams.set('code_challenge_method', 'S256');
  url.searchParams.set('prompt', 'select_account');

  const response = NextResponse.redirect(url.toString());
  response.cookies.set(OAUTH_COOKIE, JSON.stringify({ state, nonce, verifier }), {
    httpOnly: true,
    secure: secureCookies(),
    sameSite: 'lax', // sent on the top level redirect back from Microsoft
    path: '/auth/microsoft',
    maxAge: 600,
  });
  return response;
}
