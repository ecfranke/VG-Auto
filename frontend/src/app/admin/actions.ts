'use server'

import { cookies } from 'next/headers'
import { revalidatePath } from 'next/cache'
import { redirect } from 'next/navigation'
import { httpRaw } from '@/_lib/server/query-api'
import { LANG_COOKIE } from './_i18n'
import { taxesFromForm } from '@/_lib/shared/taxes'

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

const isId = (id: string) => /^[0-9a-fA-F-]{36}$/.test(id)

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
    companyId: field(form, 'companyId') || null,
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
    companyId: field(form, 'companyId') || null,
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
  redirect(form.get('returnTo')?.toString().startsWith('/admin') ? form.get('returnTo')!.toString() : '/admin')
}

function text(form: FormData, name: string): string {
  return form.get(name)?.toString() ?? ''
}

/** Full settings of any company (administrators only; the API enforces it). */
export async function saveCompany(_: ActionState, form: FormData): Promise<ActionState> {
  const companyId = field(form, 'companyId')
  if (!isId(companyId)) return { ok: false, error: 'Unknown company.' }
  const parsed = taxesFromForm(form)
  if ('error' in parsed) return { ok: false, error: parsed.error }
  const taxes = parsed.taxes
  const vatRate = Math.round(taxes.tax1Rate + taxes.tax2Rate)
  return send('PUT', `admin/companies/${companyId}/options`, {
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
        showBankAccount: form.get('showBankAccount') === 'on',
        showRegNo: form.get('showRegNo') === 'on',
        emailContent: text(form, 'emailContent'),
      },
      estimate: { emailContent: text(form, 'estimateEmailContent') },
      currency: text(form, 'currency'),
      taxes,
    },
  })
}

function emailInput(form: FormData) {
  const port = field(form, 'smtpPort')
  return {
    kind: field(form, 'kind'),
    fromAddress: field(form, 'fromAddress'),
    fromName: field(form, 'fromName'),
    smtpHost: field(form, 'smtpHost'),
    smtpPort: port ? Number(port) : null,
    smtpUser: field(form, 'smtpUser'),
    smtpPassword: text(form, 'smtpPassword'),
    smtpSecurity: field(form, 'smtpSecurity') || 'Auto',
    graphTenantId: field(form, 'graphTenantId'),
    graphClientId: field(form, 'graphClientId'),
    graphClientSecret: text(form, 'graphClientSecret'),
    graphSender: field(form, 'graphSender'),
  }
}

/** The built-in email of the system (super administrators) or the email of a company (companyId in the form). */
export async function saveEmail(_: ActionState, form: FormData): Promise<ActionState> {
  const companyId = field(form, 'companyId')
  if (companyId && !isId(companyId)) return { ok: false, error: 'Unknown company.' }
  if (field(form, 'smtpPort') && !/^\d{1,5}$/.test(field(form, 'smtpPort'))) return { ok: false, error: 'The port must be a number.' }
  return send('PUT', companyId ? `admin/companies/${companyId}/email` : 'admin/email/system', emailInput(form))
}

export async function sendTestEmail(_: ActionState, form: FormData): Promise<ActionState & { transport?: string }> {
  const companyId = field(form, 'companyId')
  if (companyId && !isId(companyId)) return { ok: false, error: 'Unknown company.' }
  const response = await httpRaw('POST', companyId ? `admin/companies/${companyId}/email/test` : 'admin/email/system/test', { to: field(form, 'to') })
  if (!response.ok) return { ok: false, error: await errorOf(response) }
  const json = await response.json()
  return { ok: true, transport: json.transport }
}

/** Allows a company to send through the built-in email, or stops it (super administrators). */
export async function allowBuiltInEmail(companyId: string, allowed: boolean): Promise<ActionState> {
  if (!isId(companyId)) return { ok: false, error: 'Unknown company.' }
  return send('PUT', `admin/companies/${companyId}/email/system`, { allowed })
}

export async function createCompany(_: ActionState, form: FormData): Promise<ActionState & { companyId?: string }> {
  const response = await httpRaw('POST', 'admin/companies', {
    name: field(form, 'name'),
    currency: field(form, 'currency'),
    allowSystemEmail: form.get('allowSystemEmail') === 'on',
  })
  if (!response.ok) return { ok: false, error: await errorOf(response) }
  const companyId = await response.json() as string
  revalidatePath('/admin', 'layout')
  redirect(`/admin/companies/${companyId}`)
}

/** Company details edited from the user page: merged into the full settings of that company. */
export async function saveCompanyInfo(_: ActionState, form: FormData): Promise<ActionState> {
  const companyId = field(form, 'companyId')
  if (!isId(companyId)) return { ok: false, error: 'Unknown company.' }
  const parsed = taxesFromForm(form)
  if ('error' in parsed) return { ok: false, error: parsed.error }
  const current = await httpRaw('GET', `admin/companies/${companyId}/options`)
  if (!current.ok) return { ok: false, error: await errorOf(current) }
  const options = await current.json()
  return send('PUT', `admin/companies/${companyId}/options`, {
    ...options,
    requisites: {
      ...options.requisites,
      name: text(form, 'name'),
      regNr: text(form, 'regNr'),
      kmkr: text(form, 'kmkr'),
      phone: text(form, 'phone'),
      email: text(form, 'email'),
      address: text(form, 'address'),
      bankAccount: text(form, 'bankAccount'),
    },
    pricing: { ...options.pricing, currency: text(form, 'currency'), taxes: parsed.taxes,
      invoice: { ...options.pricing.invoice, vatRate: Math.round(parsed.taxes.tax1Rate + parsed.taxes.tax2Rate) } },
  })
}
