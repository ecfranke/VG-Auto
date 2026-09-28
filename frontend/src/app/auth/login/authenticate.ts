'use server'
import { createSession, rememberDevice, rememberedDevice } from '@/_lib/server/session'
import { redirect } from 'next/navigation';
import { authApi, authErrorMessage } from '../_lib';

export interface LoginState {
  step: 'password' | 'code',
  challengeId?: string,
  emailHint?: string | null,
  error?: string,
  info?: string,
}

async function finish(tokens: { jwt: string, publicJwt: string, mustChangePassword: boolean, deviceToken?: string | null }) {
  await createSession(tokens.jwt, tokens.publicJwt);
  await rememberDevice(tokens.deviceToken);
  redirect(tokens.mustChangePassword ? '/auth/change-password' : '/home');
}

export async function authenticate(prevState: LoginState, formData: FormData): Promise<LoginState> {
  const intent = formData.get('intent')?.toString();

  if (intent === 'resend' && prevState.challengeId) {
    const result = await authApi('resend', { challengeId: prevState.challengeId });
    if (result.codeRequired) return { ...prevState, error: undefined, info: 'A new code was sent.' };
    return { ...prevState, info: undefined, error: authErrorMessage(result) };
  }

  if (intent === 'code' && prevState.challengeId) {
    const result = await authApi('verify', { challengeId: prevState.challengeId, code: formData.get('code')?.toString() ?? '' });
    if (result.tokens) await finish(result.tokens);
    return { ...prevState, info: undefined, error: authErrorMessage(result) };
  }

  const result = await authApi('login', {
    userName: formData.get('username')?.toString() ?? '',
    password: formData.get('password')?.toString() ?? '',
    deviceToken: await rememberedDevice(),
  });
  if (result.tokens) await finish(result.tokens);
  if (result.codeRequired) {
    return { step: 'code', challengeId: result.challengeId, emailHint: result.emailHint };
  }
  return { step: 'password', error: authErrorMessage(result) };
}
