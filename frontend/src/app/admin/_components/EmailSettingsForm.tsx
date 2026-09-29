'use client'

import { useState, useTransition } from 'react'
import clsx from 'clsx'
import { CheckCircleIcon } from '@heroicons/react/20/solid'
import { allowBuiltInEmail, ActionState, saveEmail, sendTestEmail } from '../actions'
import type { Dictionary } from '../_i18n'
import type { EmailKind, IEmailSettings } from '../model'
import { Alert, Field, SubmitButton } from './Fields'
import { useFormAction } from './useFormAction'

const inputClass = 'mt-1 block w-full rounded-md bg-surface px-3 py-1.5 text-sm text-gray-900 outline-1 -outline-offset-1 outline-gray-300 focus:outline-2 focus:-outline-offset-2 focus:outline-primary'

function Hint({ children }: { children: React.ReactNode }) {
  return <p className="mt-1 text-xs text-gray-500">{children}</p>
}

/** A password or client secret: never shown, empty keeps the saved one. */
function Secret({ label, name, saved, unreadable, t, required }: {
  label: string, name: string, saved: boolean, unreadable: boolean, t: Dictionary, required?: boolean
}) {
  return (
    <div>
      <Field label={label} name={name} type="password" autoComplete="new-password" required={required && (!saved || unreadable)}
        placeholder={saved && !unreadable ? '••••••••' : undefined} />
      {unreadable ? <p className="mt-1 text-xs text-red-700">{t.secretUnreadable}</p> : saved && <Hint>{t.secretSaved}</Hint>}
    </div>
  )
}

/**
 * The transport of the built-in email (mode "system") or of a company (mode "company"): built-in / server configuration,
 * SMTP, Microsoft 365 (Graph) or Gmail. Only the fields of the chosen transport are shown and sent.
 */
export function EmailSettingsForm({ t, mode, settings, companyId, systemAllowed = false, editable = true }: {
  t: Dictionary,
  mode: 'system' | 'company',
  settings: IEmailSettings,
  companyId?: string,
  /** company: a super administrator allowed the built-in email */
  systemAllowed?: boolean,
  editable?: boolean,
}) {
  const [state, onSubmit, pending] = useFormAction<ActionState>(saveEmail, {})
  const [kind, setKind] = useState<EmailKind>(settings.kind)
  const kinds: EmailKind[] = mode === 'system' ? ['config', 'smtp', 'graph', 'gmail'] : ['system', 'smtp', 'graph', 'gmail']
  const s = settings
  // secrets saved for another transport do not count
  const smtpSaved = s.hasSmtpPassword && s.kind === kind
  const graphSaved = s.hasGraphClientSecret && s.kind === kind
  const unreadable = s.unreadableSecret && s.kind === kind
  const blocked = kind === 'system' && !systemAllowed

  return (
    <form onSubmit={onSubmit} className="space-y-6">
      {companyId && <input type="hidden" name="companyId" value={companyId} />}
      <input type="hidden" name="kind" value={kind} />
      {state.error && <Alert kind="error">{state.error}</Alert>}
      {state.ok && <Alert kind="success">{t.saved}</Alert>}

      <fieldset disabled={!editable} className="space-y-6">
        <div>
          <p id="email-kind" className="text-sm/6 font-medium text-gray-900">{t.sendWith}</p>
          <div role="radiogroup" aria-labelledby="email-kind" className="mt-2 grid grid-cols-1 gap-3 sm:grid-cols-2 xl:grid-cols-4">
            {kinds.map(k => {
              const active = kind === k
              const unavailable = k === 'system' && !systemAllowed
              return (
                <button key={k} type="button" role="radio" aria-checked={active} onClick={() => setKind(k)}
                  className={clsx('relative flex flex-col rounded-lg bg-surface p-4 text-left ring-1 ring-inset disabled:cursor-not-allowed disabled:opacity-60',
                    active ? 'ring-2 ring-primary' : 'ring-gray-300 hover:ring-gray-400')}>
                  <span className="flex items-center justify-between gap-x-2">
                    <span className="text-sm font-semibold text-gray-900">{t.providers[k]}</span>
                    {active && <CheckCircleIcon className="size-5 text-link" aria-hidden="true" />}
                  </span>
                  <span className="mt-1 text-xs text-gray-500">{t.providerHints[k]}</span>
                  {unavailable && <span className="mt-2 inline-flex self-start rounded-md bg-yellow-50 px-2 py-0.5 text-xs font-medium text-yellow-800 ring-1 ring-yellow-600/20 ring-inset">{t.notEnabled}</span>}
                </button>
              )
            })}
          </div>
        </div>

        {kind === 'system' && (blocked
          ? <Alert kind="info">{t.builtInNotAllowed}</Alert>
          : <Alert kind="info">{t.builtInAllowedInfo}</Alert>)}

        {kind === 'smtp' && (
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-6">
            <Field label={t.smtpHost} name="smtpHost" defaultValue={s.smtpHost} required placeholder="smtp.example.com" className="sm:col-span-3" />
            <Field label={t.smtpPort} name="smtpPort" type="number" defaultValue={s.smtpPort?.toString()} placeholder="587" className="sm:col-span-1" />
            <div className="sm:col-span-2">
              <label htmlFor="smtpSecurity" className="block text-sm/6 font-medium text-gray-900">{t.smtpSecurity}</label>
              <select id="smtpSecurity" name="smtpSecurity" defaultValue={s.smtpSecurity} className={inputClass}>
                {(['Auto', 'StartTls', 'SslOnConnect', 'None'] as const).map(v => <option key={v} value={v}>{t.security[v]}</option>)}
              </select>
            </div>
            <Field label={t.smtpUser} name="smtpUser" defaultValue={s.smtpUser} className="sm:col-span-3" />
            <div className="sm:col-span-3">
              <Secret label={t.smtpPassword} name="smtpPassword" saved={smtpSaved} unreadable={unreadable} t={t} />
            </div>
            <div className="sm:col-span-3">
              <Field label={t.fromAddress} name="fromAddress" type="email" defaultValue={s.fromAddress} required={mode === 'system'} placeholder="noreply@example.com" />
              {mode === 'company' && <Hint>{t.fromAddressCompanyHint}</Hint>}
            </div>
            {mode === 'system' && <Field label={t.fromName} name="fromName" defaultValue={s.fromName} placeholder="VG Auto" className="sm:col-span-3" />}
          </div>
        )}

        {kind === 'gmail' && (
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
            <Field label={t.gmailAddress} name="fromAddress" type="email" defaultValue={s.fromAddress ?? s.smtpUser} required placeholder="garage@gmail.com" />
            <Secret label={t.gmailAppPassword} name="smtpPassword" saved={smtpSaved} unreadable={unreadable} t={t} required />
            {mode === 'system' && <Field label={t.fromName} name="fromName" defaultValue={s.fromName} placeholder="VG Auto" />}
            <p className="text-sm text-gray-500 sm:col-span-2">{t.gmailHint}</p>
          </div>
        )}

        {kind === 'graph' && (
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
            <Field label={t.graphTenantId} name="graphTenantId" defaultValue={s.graphTenantId} required placeholder="00000000-0000-0000-0000-000000000000" />
            <Field label={t.graphClientId} name="graphClientId" defaultValue={s.graphClientId} required placeholder="00000000-0000-0000-0000-000000000000" />
            <Secret label={t.graphClientSecret} name="graphClientSecret" saved={graphSaved} unreadable={unreadable} t={t} required />
            <Field label={t.graphSender} name="graphSender" type="email" defaultValue={s.graphSender} required placeholder="office@example.com" />
            {mode === 'system' && <Field label={t.fromName} name="fromName" defaultValue={s.fromName} placeholder="VG Auto" />}
            <p className="text-sm text-gray-500 sm:col-span-2">{t.graphHint}</p>
          </div>
        )}
      </fieldset>

      {editable && (
        <div className="flex items-center justify-end gap-x-4">
          {s.updatedAt && <span className="text-xs text-gray-500" suppressHydrationWarning>{t.lastChanged}: {new Date(s.updatedAt).toLocaleString()}</span>}
          <SubmitButton pending={pending || blocked}>{t.save}</SubmitButton>
        </div>
      )}
    </form>
  )
}

export function TestEmailForm({ t, companyId, defaultTo }: { t: Dictionary, companyId?: string, defaultTo: string }) {
  const [state, onSubmit, pending] = useFormAction<ActionState & { transport?: string }>(sendTestEmail, {})
  return (
    <form onSubmit={onSubmit} className="space-y-3">
      {companyId && <input type="hidden" name="companyId" value={companyId} />}
      {state.error && <Alert kind="error">{state.error}</Alert>}
      {state.ok && <Alert kind="success">{t.testEmailSent} {state.transport}</Alert>}
      <div className="flex flex-wrap items-end gap-3">
        <Field label={t.sendTo} name="to" id={companyId ? `to-${companyId}` : 'to'} type="email" defaultValue={defaultTo} required className="w-full sm:w-80" />
        <SubmitButton pending={pending} secondary>{t.sendTestEmail}</SubmitButton>
      </div>
    </form>
  )
}

/** Switch of a super administrator: the company may use the built-in email. */
export function AllowBuiltInSwitch({ companyId, allowed, label }: { companyId: string, allowed: boolean, label: string }) {
  const [value, setValue] = useState(allowed)
  const [error, setError] = useState<string | null>(null)
  const [pending, start] = useTransition()
  function toggle() {
    const next = !value
    setValue(next)
    setError(null)
    start(async () => {
      const result = await allowBuiltInEmail(companyId, next)
      if (!result.ok) {
        setValue(!next)
        setError(result.error ?? 'Error')
      }
    })
  }
  return (
    <span className="inline-flex flex-col items-end gap-1">
      <button type="button" role="switch" aria-checked={value} aria-label={label} title={label} disabled={pending} onClick={toggle}
        className={clsx('relative inline-flex h-6 w-11 shrink-0 cursor-pointer rounded-full p-0.5 transition-colors duration-200 ease-in-out focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary disabled:opacity-60',
          value ? 'bg-primary' : 'bg-gray-300')}>
        <span aria-hidden="true" className={clsx('size-5 rounded-full bg-white shadow-sm ring-1 ring-gray-900/5 transition duration-200 ease-in-out',
          value ? 'translate-x-5' : 'translate-x-0')} />
      </button>
      {error && <span role="alert" className="text-xs text-red-700">{error}</span>}
    </span>
  )
}
