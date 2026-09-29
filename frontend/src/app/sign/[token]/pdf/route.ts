import { NextResponse } from 'next/server'
import { signingApi, TOKEN_PATTERN } from '@/_lib/server/signing'

/** The estimate of a signing link as PDF (signed once the client signed it). */
export async function GET(_: Request, { params }: { params: Promise<{ token: string }> }) {
  const { token } = await params
  if (!TOKEN_PATTERN.test(token)) return new NextResponse('Not found', { status: 404 })
  const response = await signingApi(token, 'pdf')
  if (!response.ok) return new NextResponse('The PDF is not available.', { status: response.status === 429 ? 429 : 404 })
  return new NextResponse(await response.arrayBuffer(), {
    headers: {
      'Content-Type': 'application/pdf',
      'Content-Disposition': response.headers.get('Content-Disposition') ?? 'attachment; filename="estimate.pdf"',
      'Cache-Control': 'no-store',
      'Referrer-Policy': 'no-referrer',
    },
  })
}
