import { notFound } from 'next/navigation'
import { httpRaw } from '@/_lib/server/query-api'
import { currentAccount } from '@/_lib/server/account'
import { getDictionary } from '../../_i18n'
import { ICompanyEmail } from '../../model'
import { PageHeader } from '../../_components/Page'
import { Badge } from '../../_components/Badges'
import Tabs from '../../_components/Tabs'

/** A company: its details, its email and its employees. Administrators only reach their own company. */
export default async function CompanyLayout({ children, params }: { children: React.ReactNode, params: Promise<{ id: string }> }) {
  const me = await currentAccount()
  if (!me.isAdmin) return null
  const { id } = await params
  if (!/^[0-9a-fA-F-]{36}$/.test(id)) notFound()
  const response = await httpRaw('GET', `admin/companies/${id}/email`)
  if (!response.ok) notFound()
  const company = await response.json() as ICompanyEmail
  const { t } = await getDictionary()
  const base = `/admin/companies/${id}`

  return (
    <div className="space-y-6">
      <PageHeader title={company.companyName} back={me.isSuperAdmin ? { href: '/admin/companies', label: t.companies } : undefined}>
        {company.inUse
          ? <Badge color="blue">{company.inUse}</Badge>
          : <Badge color="yellow">{t.noEmailCompanies}</Badge>}
      </PageHeader>
      <Tabs tabs={[
        { href: base, label: t.tabDetails },
        { href: `${base}/email`, label: t.tabEmail },
        { href: `${base}/employees`, label: t.tabEmployees },
      ]} />
      {children}
    </div>
  )
}
