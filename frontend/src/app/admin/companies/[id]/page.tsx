import { notFound } from 'next/navigation'
import { httpGet, httpRaw } from '@/_lib/server/query-api'
import { currentAccount } from '@/_lib/server/account'
import { getDictionary } from '../../_i18n'
import { CompanyForm, ICompanyOptions } from '../CompanyForm'
import type { ITaxCountry } from '@/_lib/shared/taxes'

export default async function CompanyPage({ params }: { params: Promise<{ id: string }> }) {
  if (!(await currentAccount()).isAdmin) return null
  const { id } = await params
  if (!/^[0-9a-fA-F-]{36}$/.test(id)) notFound()
  const response = await httpRaw('GET', `admin/companies/${id}/options`)
  if (!response.ok) notFound()
  const options = await response.json() as ICompanyOptions
  const { t } = await getDictionary()
  const currencies = await (await httpGet('options/currencies')).json() as { code: string, name: string }[]
  const countries = await (await httpGet('options/taxregions')).json() as ITaxCountry[]
  return <CompanyForm key={id} t={t} companyId={id} options={options} currencies={currencies} countries={countries} />
}
