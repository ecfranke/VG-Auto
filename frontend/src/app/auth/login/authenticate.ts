
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
    }
  )

  if (!res.ok) {
    const responseText = await res.text();
    console.log(responseText);
    return { error: "Login failed", }
  }

  const jsonResponse = await res.json();

  if (jsonResponse.jwt && jsonResponse.publicJwt) {
    await createSession(jsonResponse.jwt,jsonResponse.publicJwt);
    // 5. Redirect user
    redirect('/home/work');
  }
  console.log("jwt missing");
  return { error: "Login failed", }
} 