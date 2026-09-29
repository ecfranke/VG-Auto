import Link from 'next/link'
import { redirect } from 'next/navigation'
import { httpGet } from '@/_lib/server/query-api'
import { currentAccount } from '@/_lib/server/account'
import { getDictionary } from '../_i18n'
import { ICompany, ISystemEmail } from '../model'
import { PageHeader, Table } from '../_components/Page'
import { Badge } from '../_components/Badges'
import { NewCompanyForm } from './CompanyForm'

/** All companies (super administrators); administrators go to their own company. */
export default async function CompaniesPage() {
  const me = await currentAccount()
  if (!me.isAdmin) return null
  if (!me.isSuperAdmin) redirect(`/admin/companies/${me.companyId}`)
  const { t } = await getDictionary()
  const companies = await (await httpGet('admin/companies')).json() as ICompany[]
  const email = await (await httpGet('admin/email/system')).json() as ISystemEmail
  const emailOf = new Map(email.companies.map(c => [c.id, c]))
  const currencies = await (await httpGet('options/currencies')).json() as { code: string, name: string }[]

  return (
    <div className="space-y-6">
      <PageHeader title={t.companies} hint={t.companiesHint} />

      <Table head={[t.companyName, t.regNo, t.currency, { label: t.employees, right: true }, { label: t.logins, right: true }, t.tabEmail]}
        empty={companies.length === 0} emptyText={t.empty}>
        {companies.map(c => {
          const mail = emailOf.get(c.id)
          return (
            <tr key={c.id} className="hover:bg-gray-50">
              <td className="px-4 py-3">
                <Link href={`/admin/companies/${c.id}`} className="font-medium text-link hover:text-link-hover">{c.name}</Link>
                {c.id === me.companyId && <span className="ml-1 text-gray-400">({t.you})</span>}
              </td>
              <td className="px-4 py-3 text-gray-700">{c.regNo || '—'}</td>
              <td className="px-4 py-3 text-gray-700">{c.currency}</td>
              <td className="px-4 py-3 text-right text-gray-700">
                <Link href={`/admin/companies/${c.id}/employees`} className="hover:text-link">{c.employees}</Link>
              </td>
              <td className="px-4 py-3 text-right text-gray-700">{c.users}</td>
              <td className="px-4 py-3">
                <Link href={`/admin/companies/${c.id}/email`}>
                  {!mail ? '—'
                    : mail.kind === 'smtp' || mail.kind === 'graph' || mail.kind === 'gmail'
                      ? <Badge color="blue">{mail.description}</Badge>
                      : mail.systemAllowed ? <Badge color="gray">{t.builtInEmail}</Badge> : <Badge color="yellow">{t.noEmailCompanies}</Badge>}
                </Link>
              </td>
            </tr>
          )
        })}
      </Table>

      <NewCompanyForm t={t} currencies={currencies} />
    </div>
  )
}
