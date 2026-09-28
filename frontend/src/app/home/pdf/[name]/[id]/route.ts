import { NextRequest } from 'next/server'
import { httpRaw } from '@/_lib/server/query-api'

/**
 * PDF of an estimate (offer) or invoice, passed through from the API with the session token.
 * Errors come back as JSON { error } so the page can show them.
 */
export async function GET(_: NextRequest, { params }: { params: Promise<{ name: string, id: string }> }) {
  const { name, id } = await params
  const kind = name.toLowerCase()
  if (!['offer', 'invoice'].includes(kind) || !/^[0-9a-fA-F-]{36}$/.test(id)) {
    return Response.json({ error: 'Not found.' }, { status: 404 })
  }
  const response = await httpRaw('GET', `pricings/${kind}/${id}/pdf`)
  if (!response.ok) {
    let error = `The PDF could not be created (error ${response.status}).`
    try {
      const json = await response.json()
      if (json?.isUserError && json.exceptionMessage) error = json.exceptionMessage
    } catch { /* not JSON */ }
    return Response.json({ error }, { status: response.status })
  }
  return new Response(await response.arrayBuffer(), {
    headers: {
      'Content-Type': 'application/pdf',
      'Cache-Control': 'no-store',
    },
  })
}
