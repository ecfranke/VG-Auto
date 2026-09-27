import { NextRequest, NextResponse } from 'next/server';
import { createSession } from '@/_lib/server/session';
import { authApi, authErrorMessage } from '../../_lib';
import { appUrl, callbackUrl, OAUTH_COOKIE } from '../oauth';

/** Redirect target registered in the Entra ID app registration: {APP_URL}/auth/microsoft/callback */
export async function GET(request: NextRequest) {
  const base = appUrl(request);
  const fail = (message: string) => {
    const response = NextResponse.redirect(`${base}/auth/login?error=${encodeURIComponent(message)}`);
    response.cookies.delete({ name: OAUTH_COOKIE, path: '/auth/microsoft' });
    return response;
  };

  const params = request.nextUrl.searchParams;
  if (params.get('error')) {
    return fail(params.get('error_description')?.split('\n')[0] ?? 'Microsoft sign in was cancelled.');
  }

  let saved: { state: string, nonce: string, verifier: string } | null = null;
  try { saved = JSON.parse(request.cookies.get(OAUTH_COOKIE)?.value ?? 'null'); } catch { saved = null; }
  const code = params.get('code');
  if (!saved || !code || params.get('state') !== saved.state) {
    return fail('Microsoft sign in expired or was started in another browser. Please try again.');
  }

  const result = await authApi('microsoft', {
    code,
    codeVerifier: saved.verifier,
    redirectUri: callbackUrl(request),
    nonce: saved.nonce,
  });

  if (result.tokens) {
    await createSession(result.tokens.jwt, result.tokens.publicJwt);
    const response = NextResponse.redirect(`${base}${result.tokens.mustChangePassword ? '/auth/change-password' : '/home/work'}`);
    response.cookies.delete({ name: OAUTH_COOKIE, path: '/auth/microsoft' });
    return response;
  }

  if (result.codeRequired && result.challengeId) {
    // first sign in with this Microsoft account: confirm the user's email address once
    const query = new URLSearchParams({
      challenge: result.challengeId,
      hint: result.emailHint ?? '',
      info: 'To link your Microsoft account, enter the code we emailed you.',
    });
    const response = NextResponse.redirect(`${base}/auth/login?${query}`);
    response.cookies.delete({ name: OAUTH_COOKIE, path: '/auth/microsoft' });
    return response;
  }

  return fail(authErrorMessage(result, 'Microsoft sign in failed.'));
}
