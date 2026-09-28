import Link from 'next/link'
import { notFound } from 'next/navigation'
import { httpGet, httpRaw } from '@/_lib/server/query-api'
import { currentAccount } from '@/_lib/server/account'
import { getDictionary } from '../../_i18n'
import { CompanyForm, ICompanyOptions } from '../CompanyForm'

export default async function CompanyPage({ params }: { params: Promise<{ id: string }> }) {
  if (!(await currentAccount()).isAdmin) return null
  const { id } = await params
  if (!/^[0-9a-fA-F-]{36}$/.test(id)) notFound()
  const response = await httpRaw('GET', `admin/companies/${id}/options`)
  if (!response.ok) notFound()
  const options = await response.json() as ICompanyOptions
  const { t } = await getDictionary()
  const currencies = await (await httpGet('options/currencies')).json() as { code: string, name: string }[]
  return (
    <div className="space-y-6">
      <div>
        <Link href="/admin/companies" className="text-sm text-gray-500 hover:text-gray-700">← {t.companies}</Link>
        <h1 className="mt-2 text-xl font-semibold text-gray-900">{options.requisites.name}</h1>
      </div>
      <CompanyForm key={id} t={t} companyId={id} options={options} currencies={currencies} />
    </div>
  )
}
