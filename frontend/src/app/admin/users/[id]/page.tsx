import Link from 'next/link'
import { notFound } from 'next/navigation'
import { httpGet, httpRaw } from '@/_lib/server/query-api'
import { currentAccount } from '@/_lib/server/account'
import { getDictionary } from '../../_i18n'
import { IAdminUser, ICompany } from '../../model'
import type { ITaxCountry } from '@/_lib/shared/taxes'
import { RoleBadges, StatusBadges } from '../../_components/Badges'
import { Alert } from '../../_components/Fields'
import { AccountButton, CompanyInfoForm, CreateAccountForm, ICompanyInfo, ProfileForm, ResetPasswordForm, RoleForm } from './UserForms'

function Section({ title, children }: { title: string, children: React.ReactNode }) {
  return (
    <section className="rounded-lg bg-white p-6 shadow-sm ring-1 ring-gray-900/5">
      <h2 className="mb-4 text-base font-semibold text-gray-900">{title}</h2>
      {children}
    </section>
  )
}

const can0 = (user: IAdminUser, action: string) => user.allowedActions.includes(action)

export default async function UserPage({ params }: { params: Promise<{ id: string }> }) {
  const me = await currentAccount()
  if (!me.isAdmin) return null
  const { id } = await params
  if (!/^[0-9a-fA-F-]{36}$/.test(id)) notFound()
  const response = await httpRaw('GET', `admin/users/${id}`)
  if (!response.ok) notFound()
  const user = await response.json() as IAdminUser
  const { t } = await getDictionary()
  const companies = can0(user, 'ChangeCompany') ? await (await httpGet('admin/companies')).json() as ICompany[] : []
  const companyResponse = await httpRaw('GET', `admin/companies/${user.companyId}/options`)
  const companyInfo = companyResponse.ok ? await companyResponse.json() as ICompanyInfo : null
  const currencies = companyInfo ? await (await httpGet('options/currencies')).json() as { code: string, name: string }[] : []
  const countries = companyInfo ? await (await httpGet('options/taxregions')).json() as ITaxCountry[] : []
  const can = (action: string) => user.allowedActions.includes(action)
  const creatableRoles = me.role === 'superadmin' ? ['user', 'admin', 'superadmin'] : ['user']
  const readOnly = user.hasAccount && !user.isSelf && !user.isOwner && user.allowedActions.length === 0

  return (
    <div className="space-y-6">
      <div>
        <Link href="/admin/users" className="text-sm text-gray-500 hover:text-gray-700">← {t.users}</Link>
        <div className="mt-2 flex flex-wrap items-center gap-3">
          <h1 className="text-xl font-semibold text-gray-900">{user.firstName} {user.lastName}</h1>
          <RoleBadges user={user} t={t} />
          <StatusBadges user={user} t={t} />
        </div>
      </div>

      {user.isOwner && !user.isSelf && <Alert kind="info">{t.ownerHint}</Alert>}
      {user.isSelf && <Alert kind="info">{t.selfHint}</Alert>}
      {readOnly && <Alert kind="info">{t.noRightsHint}</Alert>}

      <Section title={t.profileSection}>
        <ProfileForm user={user} t={t} companies={companies} />
      </Section>

      {companyInfo && (
        <section className="rounded-lg bg-white p-6 shadow-sm ring-1 ring-gray-900/5">
          <div className="mb-4 flex flex-wrap items-baseline justify-between gap-2">
            <h2 className="text-base font-semibold text-gray-900">{t.companyInfoOf} · {companyInfo.requisites.name}</h2>
            <Link href={`/admin/companies/${user.companyId}`} className="text-sm font-semibold text-indigo-600 hover:text-indigo-500">{t.allSettings} →</Link>
          </div>
          <CompanyInfoForm t={t} companyId={user.companyId} info={companyInfo} currencies={currencies} countries={countries} />
        </section>
      )}

      <Section title={t.accountSection}>
        {!user.hasAccount && (can('CreateAccount')
          ? <CreateAccountForm user={user} t={t} roles={creatableRoles} />
          : <p className="text-sm text-gray-500">{t.noLogin}</p>)}

        {user.hasAccount && !['ChangeRole', 'ResetPassword', 'Unlock', 'UnlinkMicrosoft', 'Disable', 'Enable'].some(a => can(a)) && (
          <p className="text-sm text-gray-500">{t.noActions}</p>
        )}
        {user.hasAccount && (
          <div className="space-y-8">
            {can('ChangeRole') && <RoleForm user={user} t={t} />}
            {can('ResetPassword') && <ResetPasswordForm user={user} t={t} />}
            <div className="flex flex-wrap gap-3">
              {user.locked && can('Unlock') && <AccountButton user={user} action="unlock" label={t.unlock} />}
              {user.microsoftLinked && can('UnlinkMicrosoft') && <AccountButton user={user} action="microsoft" label={t.unlinkMicrosoft} />}
              {!user.disabled && can('Disable') && <AccountButton user={user} action="disable" label={t.disable} danger confirmText={t.confirmDisable} />}
              {user.disabled && can('Enable') && <AccountButton user={user} action="enable" label={t.enable} />}
            </div>
            {can('Disable') && <p className="text-sm text-gray-500">{t.disableHint}</p>}
          </div>
        )}
      </Section>
    </div>
  )
}
