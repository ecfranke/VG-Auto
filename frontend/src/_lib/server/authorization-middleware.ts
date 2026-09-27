import { NextResponse, NextRequest } from 'next/server'
import { getJwt } from '@/_lib/server/session'

/**
 * Public origin of the app. Behind a reverse proxy (nginx, Baota) the URL Next.js sees is the internal
 * address (localhost:3000), so redirects use APP_URL, or the Host / X-Forwarded-* headers of the proxy.
 */
function publicOrigin(request: NextRequest): string {
  const configured = process.env.APP_URL?.trim()
  if (configured) return configured.replace(/\/$/, '')
  const host = request.headers.get('x-forwarded-host') ?? request.headers.get('host')
  if (!host) return request.nextUrl.origin
  const proto = request.headers.get('x-forwarded-proto')?.split(',')[0].trim() ?? request.nextUrl.protocol.replace(':', '')
  return `${proto}://${host.split(',')[0].trim()}`
}

export default async function authorizationMiddleware(request: NextRequest, response: NextResponse) {
  const redirectTo = (path: string) => NextResponse.redirect(new URL(path, publicOrigin(request)))
  const path = request.nextUrl.pathname
  const isProtectedRoute = path.startsWith('/home') || path.startsWith('/admin')

  // logout: remove the session cookies and go to the login page
  if (path.includes('/home/logout')) {
    const logout = redirectTo('/auth/login')
    for (const name of ['session', 'jwt', 'session_timestamp']) logout.cookies.delete(name)
    return logout
  }

  const jwt = await getJwt()
  if (path.startsWith('/auth/change-password') && !jwt) {
    return redirectTo('/auth/login')
  }
  // not signed in
  if (isProtectedRoute && !jwt) {
    return redirectTo('/auth/login')
  }
  // signed in users skip the auth pages (the forced password change page stays reachable)
  if (!isProtectedRoute && jwt && !path.startsWith('/auth/change-password')) {
    return redirectTo('/home/work')
  }

  return response
}
