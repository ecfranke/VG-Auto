import 'server-only'
import { headers } from 'next/headers'

/** A link to sign an estimate: 256 random bits, base64url. */
export const TOKEN_PATTERN = /^[A-Za-z0-9_-]{43}$/

export interface ISigning {
  /** open: can be signed; signed; expired */
  status: 'open' | 'signed' | 'expired'
  companyName: string
  companyEmail: string | null
  companyPhone: string | null
  code: string
  clientName: string | null
  vehicleTitle: string | null
  issuedOn: string
  total: string
  validUntil: string
  signerName: string | null
  signedAt: string | null
  /** the estimate as printed, rendered by the API (with the signature once signed) */
  html: string
}

/** Address of the client's browser (as our reverse proxy passed it), kept with the signature. */
export async function clientIp(): Promise<string | null> {
  const h = await headers()
  return h.get('x-real-ip')?.trim() || h.get('x-forwarded-for')?.split(',').pop()?.trim() || null
}

/** Calls the public signing API with the server secret; the page has no signed in user. */
export async function signingApi(token: string, action: 'view' | 'sign' | 'pdf', body: Record<string, unknown> = {}): Promise<Response> {
  const requestHeaders: Record<string, string> = { 'Content-Type': 'application/json' }
  const ip = await clientIp()
  if (ip) requestHeaders['X-Forwarded-For'] = ip
  return fetch(`${process.env.API_URL}/api/public/estimates/${token}/${action}`, {
    method: 'POST',
    headers: requestHeaders,
    body: JSON.stringify({ ...body, serverSecret: process.env.SERVER_SECRET }),
    cache: 'no-store',
  })
}

/** The message of an API error, or a fallback. */
export async function errorMessage(response: Response, fallback: string): Promise<string> {
  if (response.status === 429) return 'Too many attempts, please wait a minute.'
  try {
    const json = await response.json()
    return json.exceptionMessage ?? fallback
  } catch {
    return fallback
  }
}
