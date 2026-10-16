import { NextRequest, NextResponse } from 'next/server'

/** /lang?to=zh|en: remembers the language of the landing page for a year and goes back to it. */
export function GET(request: NextRequest) {
  const to = request.nextUrl.searchParams.get('to') === 'zh' ? 'zh' : 'en'
  // a relative Location: behind the reverse proxy request.url is the internal address
  const response = new NextResponse(null, { status: 303, headers: { Location: '/' } })
  response.cookies.set('site_lang', to, { path: '/', maxAge: 60 * 60 * 24 * 365, sameSite: 'lax' })
  return response
}
