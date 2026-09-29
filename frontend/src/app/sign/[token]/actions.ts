'use server'

import { revalidatePath } from 'next/cache'
import { clientIp, errorMessage, signingApi, TOKEN_PATTERN } from '@/_lib/server/signing'

export interface SignState {
  ok?: boolean
  error?: string
}

/** Signs the estimate of the link with the drawn signature (a PNG data URL). */
export async function signEstimate(token: string, name: string, accepted: boolean, signature: string): Promise<SignState> {
  if (!TOKEN_PATTERN.test(token)) return { error: 'This link is not valid.' }
  if (!name.trim()) return { error: 'Please enter your name.' }
  if (!accepted) return { error: 'Please confirm that you accept the estimate.' }
  if (!signature) return { error: 'Please sign in the box.' }
  const response = await signingApi(token, 'sign', { name: name.trim(), accepted, signature, clientIp: await clientIp() })
  if (!response.ok) return { error: await errorMessage(response, 'The estimate could not be signed. Please try again.') }
  revalidatePath(`/sign/${token}`)
  return { ok: true }
}
