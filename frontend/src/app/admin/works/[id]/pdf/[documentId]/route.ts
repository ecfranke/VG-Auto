import { NextRequest } from 'next/server'
import { httpRaw } from '@/_lib/server/query-api'

/** An estimate or invoice of any company's work as PDF (super administrators; the API checks it). */
export async function GET(_: NextRequest, { params }: { params: Promise<{ id: string, documentId: string }> }) {
  const { id, documentId } = await params
  if (!/^[0-9a-fA-F-]{36}$/.test(id) || !/^[0-9a-fA-F-]{36}$/.test(documentId)) {
    return Response.json({ error: 'Not found.' }, { status: 404 })
  }
  const response = await httpRaw('GET', `admin/works/${id}/pdf/${documentId}`)
  if (!response.ok) return Response.json({ error: `The PDF could not be created (error ${response.status}).` }, { status: response.status })
  return new Response(await response.arrayBuffer(), {
    headers: {
      'Content-Type': 'application/pdf',
      'Content-Disposition': response.headers.get('Content-Disposition') ?? 'inline',
      'Cache-Control': 'no-store',
    },
  })
}
