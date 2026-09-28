'use client'

import { useActionState, useState } from 'react'
import Link from 'next/link'
import { createUser, ActionState } from '../../actions'
import type { Dictionary } from '../../_i18n'
import { Alert, CompanySelect, Field, RoleSelect, SubmitButton, TemporaryPassword } from '../../_components/Fields'
import type { ICompany } from '../../model'

export default function NewUserForm({ t, roles, companies, defaultCompany }: { t: Dictionary, roles: string[], companies: ICompany[], defaultCompany: string }) {
  const [state, action, pending] = useActionState<ActionState, FormData>(createUser, {})
  const [withLogin, setWithLogin] = useState(true)

  if (state.ok && state.employeeId) {
    return (
      <div className="space-y-4">
        <Alert kind="success">{t.created}</Alert>
        {state.temporaryPassword && <TemporaryPassword password={state.temporaryPassword} title={t.temporaryPassword} hint={t.temporaryPasswordHint} />}
        <Link href={`/admin/users/${state.employeeId}`} className="inline-block text-sm font-semibold text-indigo-600 hover:text-indigo-500">{t.openUser} →</Link>
      </div>
    )
  }

  return (
    <form action={action} className="space-y-6">
      {state.error && <Alert kind="error">{state.error}</Alert>}
      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
        <Field label={t.firstName} name="firstName" required />
        <Field label={t.lastName} name="lastName" required />
        <Field label={t.email} name="email" type="email" required={withLogin} />
        <Field label={t.phone} name="phone" />
        <Field label={t.profession} name="profession" />
        <Field label={t.description} name="description" />
        <CompanySelect label={t.companyOf} companies={companies} defaultValue={defaultCompany} />
      </div>

      <div className="border-t border-gray-100 pt-6">
        <label className="flex items-center gap-x-2 text-sm font-medium text-gray-900">
          <input type="checkbox" name="createAccount" checked={withLogin} onChange={e => setWithLogin(e.target.checked)}
            className="size-4 rounded border-gray-300 text-indigo-600 focus:ring-indigo-600" />
          {t.createLogin}
        </label>
        {withLogin && (
          <div className="mt-4 grid grid-cols-1 gap-4 sm:grid-cols-2">
            <Field label={t.userName} name="userName" required />
            <RoleSelect label={t.role} roles={roles.map(r => ({ value: r, label: t.roles[r] }))} />
            <Field label={t.passwordOptional} name="password" type="password" autoComplete="new-password" className="sm:col-span-2" />
            <p className="text-sm text-gray-500 sm:col-span-2">{t.passwordHint}</p>
          </div>
        )}
      </div>

      <div className="flex justify-end gap-x-3">
        <Link href="/admin/users" className="rounded-md px-3 py-2 text-sm font-semibold text-gray-900 hover:bg-gray-100">{t.cancel}</Link>
        <SubmitButton pending={pending}>{t.save}</SubmitButton>
      </div>
    </form>
  )
}
