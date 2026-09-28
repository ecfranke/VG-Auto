'use server'
import { redirect } from 'next/navigation';
import { httpPut } from '@/_lib/server/query-api';
import { createSession, getJwt } from '@/_lib/server/session';

export async function changeInitialPassword(prevState: { error: string }, formData: FormData)
  : Promise<{ error: string }> {

  if (!(await getJwt())) redirect('/auth/login');

  const res = await httpPut({
    url: 'profile/changepassword',
    body: {
      currentPassword: formData.get('currentPassword'),
      newPassword: formData.get('newPassword'),
      confirmPassword: formData.get('confirmPassword'),
    },
    raw: true,
  });

  if (!res.ok) {
    try {
      const json = await res.json();
      return { error: json.exceptionMessage ?? 'Password change failed' };
    } catch {
      return { error: 'Password change failed' };
    }
  }

  const tokens = await res.json();
  await createSession(tokens.jwt, tokens.publicJwt);
  redirect('/home');
}
