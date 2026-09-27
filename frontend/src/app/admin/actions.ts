'use server'

import { cookies } from 'next/headers'
import { revalidatePath } from 'next/cache'
import { redirect } from 'next/navigation'
import { httpRaw } from '@/_lib/server/query-api'
import { LANG_COOKIE } from './_i18n'

export interface ActionState {
  ok?: boolean
  error?: string
  temporaryPassword?: string | null
  employeeId?: string
}

async function errorOf(response: Response): Promise<string> {
  const text = await response.text()
  try {
    const json = JSON.parse(text)
    return json.exceptionMessage ?? json.message ?? json.title ?? `Error ${response.status}`
  } catch {
    return `Error ${response.status}`
  }
}

function field(form: FormData, name: string): string {
  return (form.get(name)?.toString() ?? '').trim()
}

async function send(method: 'POST' | 'PUT' | 'DELETE', url: string, body: unknown, employeeId?: string): Promise<ActionState> {
  const response = await httpRaw(method, url, body)
  if (!response.ok) return { ok: false, error: await errorOf(response) }
  const text = await response.text()
  const json = text ? JSON.parse(text) : {}
  revalidatePath('/admin', 'layout')
  return { ok: true, temporaryPassword: json?.temporaryPassword ?? null, employeeId: json?.employeeId ?? employeeId }
}

export async function createUser(_: ActionState, form: FormData): Promise<ActionState> {
  const createAccount = form.get('createAccount') === 'on'
  return send('POST', 'admin/users', {
    firstName: field(form, 'firstName'),
    lastName: field(form, 'lastName'),
    email: field(form, 'email'),
    phone: field(form, 'phone'),
    profession: field(form, 'profession'),
    description: field(form, 'description'),
    createAccount,
    userName: createAccount ? field(form, 'userName') : null,
    password: createAccount ? field(form, 'password') : null,
    role: field(form, 'role') || 'user',
  })
}

export async function editUser(_: ActionState, form: FormData): Promise<ActionState> {
  const id = field(form, 'employeeId')
  return send('PUT', `admin/users/${id}`, {
    firstName: field(form, 'firstName'),
    lastName: field(form, 'lastName'),
    email: field(form, 'email'),
    phone: field(form, 'phone'),
    profession: field(form, 'profession'),
    description: field(form, 'description'),
    userName: field(form, 'userName') || null,
  }, id)
}

export async function createAccount(_: ActionState, form: FormData): Promise<ActionState> {
  const id = field(form, 'employeeId')
  return send('POST', `admin/users/${id}/account`, {
    userName: field(form, 'userName'),
    password: field(form, 'password'),
    role: field(form, 'role') || 'user',
  }, id)
}

export async function resetPassword(_: ActionState, form: FormData): Promise<ActionState> {
  const id = field(form, 'employeeId')
  return send('POST', `admin/users/${id}/password`, { password: field(form, 'password') }, id)
}

export async function changeRole(_: ActionState, form: FormData): Promise<ActionState> {
  const id = field(form, 'employeeId')
  return send('PUT', `admin/users/${id}/role`, { role: field(form, 'role') }, id)
}

/** unlock | disable | enable | microsoft */
export async function accountAction(_: ActionState, form: FormData): Promise<ActionState> {
  const id = field(form, 'employeeId')
  const action = field(form, 'action')
  switch (action) {
    case 'unlock':
    case 'disable':
    case 'enable':
      return send('POST', `admin/users/${id}/${action}`, null, id)
    case 'microsoft':
      return send('DELETE', `admin/users/${id}/microsoft`, null, id)
  }
  return { ok: false, error: 'Unknown action' }
}

export async function setLanguage(form: FormData) {
  const lang = form.get('lang') === 'zh' ? 'zh' : 'en'
  ;(await cookies()).set(LANG_COOKIE, lang, { path: '/admin', maxAge: 60 * 60 * 24 * 365, sameSite: 'lax' })
  redirect(form.get('returnTo')?.toString().startsWith('/admin') ? form.get('returnTo')!.toString() : '/admin/users')
}

function text(form: FormData, name: string): string {
  return form.get(name)?.toString() ?? ''
}

/** Full company settings (administrators only; the API enforces it). */
export async function saveCompany(_: ActionState, form: FormData): Promise<ActionState> {
  const vatRate = Number(text(form, 'vatRate'))
  if (!Number.isInteger(vatRate) || vatRate < 0 || vatRate > 100) return { ok: false, error: 'VAT rate must be a whole number between 0 and 100.' }
  return send('PUT', 'options', {
    requisites: {
      name: text(form, 'name'),
      phone: text(form, 'phone'),
      address: text(form, 'address'),
      email: text(form, 'email'),
      bankAccount: text(form, 'bankAccount'),
      regNr: text(form, 'regNr'),
      kmkr: text(form, 'kmkr'),
    },
    pricing: {
      invoice: {
        vatRate,
        surCharge: text(form, 'surCharge'),
        disclaimer: text(form, 'disclaimer'),
        signatureLine: form.get('signatureLine') === 'on',
        emailContent: text(form, 'emailContent'),
      },
      estimate: { emailContent: text(form, 'estimateEmailContent') },
      currency: text(form, 'currency'),
    },
  })
}

export async function sendTestEmail(_: ActionState, form: FormData): Promise<ActionState & { transport?: string }> {
  const response = await httpRaw('POST', 'options/testemail', { to: field(form, 'to') })
  if (!response.ok) return { ok: false, error: await errorOf(response) }
  const json = await response.json()
  return { ok: true, transport: json.transport }
}
