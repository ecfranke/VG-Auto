
'use server'
import { createSession } from '@/_lib/server/session'
import { redirect } from 'next/navigation';
import { httpPost } from '@/_lib/server/query-api';

export async function authenticate(prevState: { error: string }, formData: FormData)
  : Promise<{ error: string }> {

  const res = await httpPost(
    {
      url: 'users/authenticate',
      body: {
        username: formData.get('username'),
        password: formData.get('password'),
        serverSecret: process.env.SERVER_SECRET
      },
      authorize: false,
      raw: true,
    }
  )

  if (!res.ok) {
    if (res.status === 429) return { error: "Too many attempts, please wait a minute." };
    try {
      const json = await res.json();
      if (json?.locked) return { error: "Account temporarily locked after too many failed attempts." };
    } catch { /* no body */ }
    return { error: "Wrong username or password" }
  }

  const jsonResponse = await res.json();

  if (jsonResponse.jwt && jsonResponse.publicJwt) {
    await createSession(jsonResponse.jwt,jsonResponse.publicJwt);
    redirect(jsonResponse.mustChangePassword ? '/auth/change-password' : '/home/work');
  }
  console.log("jwt missing");
  return { error: "Login failed", }
} 