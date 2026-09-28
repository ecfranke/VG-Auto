import 'server-only'
import { httpPost } from '@/_lib/server/query-api';

export interface AuthApiResult {
  ok: boolean,
  status: number,
  tokens?: { jwt: string, publicJwt: string, mustChangePassword: boolean, deviceToken?: string | null },
  codeRequired?: boolean,
  challengeId?: string,
  emailHint?: string | null,
  error?: string,
  message?: string | null,
}

/** Calls one of the /api/auth endpoints with the server secret. */
export async function authApi(path: string, body: Record<string, unknown>): Promise<AuthApiResult> {
  const res = await httpPost({
    url: `auth/${path}`,
    body: { ...body, serverSecret: process.env.SERVER_SECRET },
    authorize: false,
    raw: true,
  });
  let json: Record<string, unknown> = {};
  try { json = await res.json(); } catch { /* empty body */ }
  if (res.ok && json.jwt) {
    return { ok: true, status: res.status, tokens: json as unknown as AuthApiResult['tokens'] };
  }
  if (res.ok && json.codeRequired) {
    return { ok: true, status: res.status, codeRequired: true, challengeId: json.challengeId as string, emailHint: json.emailHint as string | null };
  }
  return { ok: res.ok, status: res.status, error: json.error as string, message: json.message as string | null };
}

export interface AuthProviders {
  passwordReset: boolean,
  emailCode: boolean,
  microsoft: { enabled: boolean, clientId: string | null, authorizeUrl: string | null },
}

export async function getProviders(): Promise<AuthProviders> {
  try {
    const res = await fetch(`${process.env.API_URL}/api/auth/providers`, { cache: 'no-store' });
    if (!res.ok) throw new Error(res.statusText);
    return await res.json();
  } catch {
    return { passwordReset: false, emailCode: false, microsoft: { enabled: false, clientId: null, authorizeUrl: null } };
  }
}

/** Human readable message for an API error. */
export function authErrorMessage(result: AuthApiResult, fallback = 'Sign in failed'): string {
  if (result.status === 429) return 'Too many attempts, please wait a minute.';
  switch (result.error) {
    case 'locked': return 'Account temporarily locked after too many failed attempts. Try again in 15 minutes.';
    case 'invalidCredentials': return 'Wrong username or password.';
    case 'invalidCode': return 'The code is wrong or has expired.';
    case 'tooManyRequests': return 'No more codes can be sent. Start again.';
    case 'emailFailed': return result.message ?? 'The code could not be sent by email.';
  }
  return result.message ?? fallback;
}
