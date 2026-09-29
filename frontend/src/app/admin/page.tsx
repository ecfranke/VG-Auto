import Link from 'next/link'
import { redirect } from 'next/navigation'
import { httpGet } from '@/_lib/server/query-api'
import { currentAccount } from '@/_lib/server/account'
import { getDictionary } from './_i18n'
import { ICompany, IOverview } from './model'
import { Card, PageHeader, Stat } from './_components/Page'
import { Badge } from './_components/Badges'

/** Super administrators: the whole system at a glance. Administrators go to their company. */
export default async function AdminHome() {
  const me = await currentAccount()
  if (!me.isAdmin) return null
  if (!me.isSuperAdmin) redirect(`/admin/companies/${me.companyId}`)
  const { t, lang } = await getDictionary()
  const overview = await (await httpGet('admin/overview')).json() as IOverview
  const companies = await (await httpGet('admin/companies')).json() as ICompany[]
  const names = new Map(companies.map(c => [c.id, c.name]))
  const format = new Intl.DateTimeFormat(lang === 'zh' ? 'zh-CN' : 'en-GB', { dateStyle: 'medium', timeStyle: 'short' })

  return (
    <div className="space-y-8">
      <PageHeader title={t.overview} hint={t.overviewHint} />

      <dl className="grid grid-cols-2 gap-4 md:grid-cols-3 xl:grid-cols-5">
        <Stat label={t.companies} value={overview.companies} href="/admin/companies" />
        <Stat label={t.employees} value={overview.employees} href="/admin/users" />
        <Stat label={t.logins} value={overview.logins} href="/admin/users" />
        <Stat label={t.administrators} value={overview.administrators} href="/admin/users" />
        <Stat label={t.disabledLogins} value={overview.disabledLogins} href="/admin/users" />
      </dl>

      <div className="grid grid-cols-1 gap-6 lg:grid-cols-2">
        <Card title={t.builtInEmail} actions={<Link href="/admin/email" className="text-sm font-semibold text-link hover:text-link-hover">{t.configure} →</Link>}>
          <div className="text-sm text-gray-500">{t.inUse}</div>
          <div className="mt-1 flex flex-wrap items-center gap-2">
            <span className="font-medium text-gray-900">{overview.systemEmail}</span>
            <Badge color={overview.systemEmailSaved ? 'blue' : 'gray'}>{overview.systemEmailSaved ? t.setInAdmin : t.providers.config}</Badge>
          </div>
        </Card>

        <Card title={t.companyEmails}>
          <dl className="grid grid-cols-3 gap-4 text-center">
            <div><dt className="text-xs text-gray-500">{t.builtInCompanies}</dt><dd className="mt-1 text-2xl font-semibold text-gray-900">{overview.builtInEmailCompanies}</dd></div>
            <div><dt className="text-xs text-gray-500">{t.ownEmailCompanies}</dt><dd className="mt-1 text-2xl font-semibold text-gray-900">{overview.ownEmailCompanies}</dd></div>
            <div><dt className="text-xs text-gray-500">{t.noEmailCompanies}</dt>
              <dd className={overview.noEmailCompanies > 0 ? 'mt-1 text-2xl font-semibold text-yellow-700' : 'mt-1 text-2xl font-semibold text-gray-900'}>{overview.noEmailCompanies}</dd></div>
          </dl>
        </Card>
      </div>

      <Card title={t.recentActivity} actions={<Link href="/admin/audit" className="text-sm font-semibold text-link hover:text-link-hover">{t.viewAll} →</Link>}>
        {overview.recent.length === 0 ? <p className="text-sm text-gray-500">{t.empty}</p> : (
          <ul role="list" className="-my-3 divide-y divide-gray-100">
            {overview.recent.map(entry => (
              <li key={entry.id} className="flex flex-wrap items-baseline justify-between gap-x-4 gap-y-1 py-3 text-sm">
                <div className="min-w-0">
                  <span className="font-medium text-gray-900">{t.actions[entry.action] ?? entry.action}</span>
                  {entry.target && <span className="text-gray-700"> · {entry.target}</span>}
                  <div className="truncate text-xs text-gray-500">
                    {entry.actor}
                    {entry.companyId && names.has(entry.companyId) && <> · {names.get(entry.companyId)}</>}
                    {entry.details && <> · {entry.details}</>}
                  </div>
                </div>
                <time className="shrink-0 text-xs text-gray-500" dateTime={entry.createdAt}>{format.format(new Date(entry.createdAt))}</time>
              </li>
            ))}
          </ul>
        )}
      </Card>
    </div>
  )
}
