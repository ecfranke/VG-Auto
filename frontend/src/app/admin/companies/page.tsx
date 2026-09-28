import Link from 'next/link'
import { httpGet } from '@/_lib/server/query-api'
import { currentAccount } from '@/_lib/server/account'
import { getDictionary } from '../_i18n'
import { ICompany } from '../model'
import { NewCompanyForm, TestEmailForm } from './CompanyForm'

export default async function CompaniesPage() {
  const me = await currentAccount()
  if (!me.isAdmin) return null
  const { t } = await getDictionary()
  const companies = await (await httpGet('admin/companies')).json() as ICompany[]
  const currencies = await (await httpGet('options/currencies')).json() as { code: string, name: string }[]
  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-xl font-semibold text-gray-900">{t.companies}</h1>
        <p className="mt-1 text-sm text-gray-500">{t.companiesHint}</p>
      </div>

      <div className="overflow-x-auto rounded-lg bg-white shadow-sm ring-1 ring-gray-900/5">
        <table className="min-w-full divide-y divide-gray-200 text-sm">
          <thead className="bg-gray-50 text-left text-gray-900">
            <tr>
              <th className="px-4 py-3 font-semibold">{t.companyName}</th>
              <th className="px-4 py-3 font-semibold">{t.regNo}</th>
              <th className="px-4 py-3 font-semibold">{t.currency}</th>
              <th className="px-4 py-3 text-right font-semibold">{t.employees}</th>
              <th className="px-4 py-3 text-right font-semibold">{t.logins}</th>
              <th className="px-4 py-3"></th>
            </tr>
          </thead>
          <tbody className="divide-y divide-gray-100">
            {companies.map(c => (
              <tr key={c.id} className="hover:bg-gray-50">
                <td className="px-4 py-3">
                  <Link href={`/admin/companies/${c.id}`} className="font-medium text-indigo-600 hover:text-indigo-500">{c.name}</Link>
                  {c.id === me.companyId && <span className="ml-1 text-gray-400">({t.you})</span>}
                </td>
                <td className="px-4 py-3 text-gray-700">{c.regNo || '—'}</td>
                <td className="px-4 py-3 text-gray-700">{c.currency}</td>
                <td className="px-4 py-3 text-right text-gray-700">{c.employees}</td>
                <td className="px-4 py-3 text-right text-gray-700">{c.users}</td>
                <td className="px-4 py-3 text-right">
                  <Link href={`/admin/companies/${c.id}`} className="text-sm font-semibold text-indigo-600 hover:text-indigo-500">{t.editCompany}</Link>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <NewCompanyForm t={t} currencies={currencies} />
      <TestEmailForm t={t} defaultTo={me.email ?? ''} />
    </div>
  )
}
