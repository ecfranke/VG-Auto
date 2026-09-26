import 'server-only'
import { cookies } from 'next/headers' 

export async function pushToast(message: string,isError?: boolean | undefined) {
  const expiresAt = new Date(Date.now() + 30 * 1000)
  const json =  {
    message,
    isError
  }
  const cookieStore = await cookies() 
  cookieStore.set('toast', JSON.stringify(json), {
    httpOnly: false,
    secure: true,
    expires: expiresAt,
    sameSite: 'lax',
    path: '/',
  })
}