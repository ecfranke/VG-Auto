'use client'

import { useActionState } from 'react'
import { accountAction, ActionState, changeRole, createAccount, editUser, resetPassword, saveCompanyInfo } from '../../actions'
import type { Dictionary } from '../../_i18n'
import type { IAdminUser } from '../../model'
import { Alert, CompanySelect, Field, RoleSelect, SubmitButton, TemporaryPassword } from '../../_components/Fields'

type Props = { user: IAdminUser, t: Dictionary }
const can = (user: IAdminUser, action: string) => user.allowedActions.includes(action)

type Company = { id: string, name: string }

export function ProfileForm({ user, t, companies }: Props & { companies: Company[] }) {
  const [state, action, pending] = useActionState<ActionState, FormData>(editUser, {})
  const editable = can(user, 'EditProfile')
  const movable = can(user, 'ChangeCompany')
  const confirmMove = (e: React.FormEvent<HTMLFormElement>) => {
    const selected = new FormData(e.currentTarget).get('companyId')
    if (movable && selected && selected !== user.companyId && !window.confirm(t.confirmMove)) e.preventDefault()
  }
  return (
    <form action={action} onSubmit={confirmMove} className="space-y-4">
      <input type="hidden" name="employeeId" value={user.employeeId} />
      {state.error && <Alert kind="error">{state.error}</Alert>}
      {state.ok && <Alert kind="success">{t.saved}</Alert>}
      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
        <Field label={t.firstName} name="firstName" defaultValue={user.firstName} required disabled={!editable} />
        <Field label={t.lastName} name="lastName" defaultValue={user.lastName} required disabled={!editable} />
        {user.hasAccount && <Field label={t.userName} name="userName" defaultValue={user.userName} required disabled={!editable} />}
        <Field label={t.email} name="email" type="email" defaultValue={user.email} required={user.hasAccount} disabled={!editable} />
        <Field label={t.phone} name="phone" defaultValue={user.phone} disabled={!editable} />
        <Field label={t.profession} name="profession" defaultValue={user.profession} disabled={!editable} />
        <Field label={t.description} name="description" defaultValue={user.description} disabled={!editable} className="sm:col-span-2" />
        {movable
          ? <div className="sm:col-span-2">
              <CompanySelect key={user.companyId} label={t.companyOf} companies={companies} defaultValue={user.companyId} />
              <p className="mt-1 text-xs text-gray-500">{t.companyFieldHint}</p>
            </div>
          : <Field label={t.companyOf} name="companyName" defaultValue={user.companyName} disabled className="sm:col-span-2" />}
      </div>
      {editable && <div className="flex justify-end"><SubmitButton pending={pending}>{t.save}</SubmitButton></div>}
    </form>
  )
}

export function CreateAccountForm({ user, t, roles }: Props & { roles: string[] }) {
  const [state, action, pending] = useActionState<ActionState, FormData>(createAccount, {})
  if (state.ok) {
    return state.temporaryPassword
      ? <TemporaryPassword password={state.temporaryPassword} title={t.temporaryPassword} hint={t.temporaryPasswordHint} />
      : <Alert kind="success">{t.saved}</Alert>
  }
  return (
    <form action={action} className="space-y-4">
      <input type="hidden" name="employeeId" value={user.employeeId} />
      {state.error && <Alert kind="error">{state.error}</Alert>}
      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
        <Field label={t.userName} name="userName" required />
        <RoleSelect label={t.role} roles={roles.map(r => ({ value: r, label: t.roles[r] }))} />
        <Field label={t.passwordOptional} name="password" type="password" autoComplete="new-password" className="sm:col-span-2" />
      </div>
      <p className="text-sm text-gray-500">{t.passwordHint}</p>
      <div className="flex justify-end"><SubmitButton pending={pending}>{t.createAccount}</SubmitButton></div>
    </form>
  )
}

export function RoleForm({ user, t }: Props) {
  const [state, action, pending] = useActionState<ActionState, FormData>(changeRole, {})
  return (
    <form action={action} className="space-y-3">
      <input type="hidden" name="employeeId" value={user.employeeId} />
      {state.error && <Alert kind="error">{state.error}</Alert>}
      {state.ok && <Alert kind="success">{t.saved}</Alert>}
      <div className="flex flex-wrap items-end gap-3">
        <div className="w-64">
          <RoleSelect label={t.role} defaultValue={user.role} roles={['user', 'admin', 'superadmin'].map(r => ({ value: r, label: t.roles[r] }))} />
        </div>
        <SubmitButton pending={pending} secondary>{t.changeRole}</SubmitButton>
      </div>
      <p className="text-sm text-gray-500">{t.roleHintAdmin} {t.roleHintSuper}</p>
    </form>
  )
}

export function ResetPasswordForm({ user, t }: Props) {
  const [state, action, pending] = useActionState<ActionState, FormData>(resetPassword, {})
  return (
    <form action={action} className="space-y-3">
      <input type="hidden" name="employeeId" value={user.employeeId} />
      {state.error && <Alert kind="error">{state.error}</Alert>}
      {state.ok && (state.temporaryPassword
        ? <TemporaryPassword password={state.temporaryPassword} title={t.temporaryPassword} hint={t.temporaryPasswordHint} />
        : <Alert kind="success">{t.saved}</Alert>)}
      <div className="flex flex-wrap items-end gap-3">
        <Field label={t.passwordOptional} name="password" type="password" autoComplete="new-password" className="w-80" />
        <SubmitButton pending={pending} secondary>{t.resetPassword}</SubmitButton>
      </div>
      <p className="text-sm text-gray-500">{t.resetPasswordHint}</p>
    </form>
  )
}

export function AccountButton({ user, action: name, label, danger, confirmText }: { user: IAdminUser } & { action: string, label: string, danger?: boolean, confirmText?: string }) {
  const [state, action, pending] = useActionState<ActionState, FormData>(accountAction, {})
  return (
    <form action={action} onSubmit={e => { if (confirmText && !window.confirm(confirmText)) e.preventDefault() }} className="inline-flex flex-col gap-2">
      <input type="hidden" name="employeeId" value={user.employeeId} />
      <input type="hidden" name="action" value={name} />
      <SubmitButton pending={pending} danger={danger} secondary={!danger}>{label}</SubmitButton>
      {state.error && <Alert kind="error">{state.error}</Alert>}
    </form>
  )
}

export interface ICompanyInfo {
  requisites: { name: string, phone: string, address: string, email: string, bankAccount: string, regNr: string, kmkr: string }
  pricing: { currency: string }
}

export function CompanyInfoForm({ t, companyId, info, currencies }: { t: Dictionary, companyId: string, info: ICompanyInfo, currencies: { code: string, name: string }[] }) {
  const [state, action, pending] = useActionState<ActionState, FormData>(saveCompanyInfo, {})
  const r = info.requisites
  return (
    <form key={companyId} action={action} className="space-y-4">
      <input type="hidden" name="companyId" value={companyId} />
      {state.error && <Alert kind="error">{state.error}</Alert>}
      {state.ok && <Alert kind="success">{t.saved}</Alert>}
      <p className="text-sm text-gray-500">{t.companyInfoHint}</p>
      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
        <Field label={t.companyName} name="name" id="company-name" defaultValue={r.name} required />
        <Field label={t.regNo} name="regNr" id="company-regNr" defaultValue={r.regNr} />
        <Field label={t.taxId} name="kmkr" id="company-kmkr" defaultValue={r.kmkr} />
        <div>
          <label htmlFor="companyCurrency" className="block text-sm/6 font-medium text-gray-900">{t.currency}</label>
          <select id="companyCurrency" name="currency" defaultValue={info.pricing.currency}
            className="mt-1 block w-full rounded-md bg-white px-3 py-1.5 text-sm text-gray-900 outline-1 -outline-offset-1 outline-gray-300 focus:outline-2 focus:-outline-offset-2 focus:outline-indigo-600">
            {currencies.map(c => <option key={c.code} value={c.code}>{c.code} · {c.name}</option>)}
          </select>
        </div>
        <Field label={t.phone} name="phone" id="company-phone" defaultValue={r.phone} />
        <Field label={t.email} name="email" id="company-email" type="email" defaultValue={r.email} />
        <Field label={t.address} name="address" id="company-address" defaultValue={r.address} className="sm:col-span-2" />
        <Field label={t.bankAccount} name="bankAccount" id="company-bankAccount" defaultValue={r.bankAccount} className="sm:col-span-2" />
      </div>
      <div className="flex justify-end"><SubmitButton pending={pending}>{t.save}</SubmitButton></div>
    </form>
  )
}
