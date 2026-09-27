import { httpGet } from '@/_lib/server/query-api'
import { currentAccount } from '@/_lib/server/account'
import { getDictionary } from '../_i18n'
import { CompanyForm, ICompanyOptions, TestEmailForm } from './CompanyForm'

export default async function CompanyPage() {
  if (!(await currentAccount()).isAdmin) return null
  const { t } = await getDictionary()
  const options = await (await httpGet('options')).json() as ICompanyOptions
  const currencies = await (await httpGet('options/currencies')).json() as { code: string, name: string }[]
  return (
    <div className="space-y-6">
      <h1 className="text-xl font-semibold text-gray-900">{t.company}</h1>
      <CompanyForm t={t} options={options} currencies={currencies} />
      <TestEmailForm t={t} defaultTo={options.requisites.email} />
    </div>
  )
}
