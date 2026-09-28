'use client'

import { useActionState } from 'react'
import { accountAction, ActionState, changeCompany, changeRole, createAccount, editUser, resetPassword } from '../../actions'
import type { Dictionary } from '../../_i18n'
import type { IAdminUser } from '../../model'
import { Alert, CompanySelect, Field, RoleSelect, SubmitButton, TemporaryPassword } from '../../_components/Fields'

type Props = { user: IAdminUser, t: Dictionary }
const can = (user: IAdminUser, action: string) => user.allowedActions.includes(action)

export function ProfileForm({ user, t }: Props) {
  const [state, action, pending] = useActionState<ActionState, FormData>(editUser, {})
  const editable = can(user, 'EditProfile')
  return (
    <form action={action} className="space-y-4">
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

export function CompanyForm({ user, t, companies }: Props & { companies: { id: string, name: string }[] }) {
  const [state, action, pending] = useActionState<ActionState, FormData>(changeCompany, {})
  return (
    <form action={action} className="space-y-3" onSubmit={e => { if (!window.confirm(t.confirmMove)) e.preventDefault() }}>
      <input type="hidden" name="employeeId" value={user.employeeId} />
      {state.error && <Alert kind="error">{state.error}</Alert>}
      {state.ok && <Alert kind="success">{t.saved}</Alert>}
      <div className="flex flex-wrap items-end gap-3">
        <div className="w-64">
          <CompanySelect key={user.companyId} label={t.companyOf} companies={companies} defaultValue={user.companyId} />
        </div>
        <SubmitButton pending={pending} secondary>{t.moveCompany}</SubmitButton>
      </div>
      <p className="text-sm text-gray-500">{t.moveCompanyHint}</p>
    </form>
  )
}
