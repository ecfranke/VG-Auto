import 'server-only'
import { NextRequest } from 'next/server';

export const OAUTH_COOKIE = 'ms_oauth';

export function base64Url(bytes: Uint8Array): string {
  return Buffer.from(bytes).toString('base64').replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');
}

export function randomToken(bytes = 32): string {
  return base64Url(crypto.getRandomValues(new Uint8Array(bytes)));
}

export async function pkceChallenge(verifier: string): Promise<string> {
  const digest = await crypto.subtle.digest('SHA-256', new TextEncoder().encode(verifier));
  return base64Url(new Uint8Array(digest));
}

/** Public base URL of the app (APP_URL), used for the OAuth redirect URI. */
export function appUrl(request: NextRequest): string {
  return (process.env.APP_URL ?? request.nextUrl.origin).replace(/\/$/, '');
}

export function callbackUrl(request: NextRequest): string {
  return `${appUrl(request)}/auth/microsoft/callback`;
}

export function secureCookies(): boolean {
  return process.env.COOKIE_SECURE ? process.env.COOKIE_SECURE === 'true' : process.env.NODE_ENV === 'production';
}
