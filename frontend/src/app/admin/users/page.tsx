import Link from 'next/link'
import { PlusIcon } from '@heroicons/react/20/solid'
import { httpGet } from '@/_lib/server/query-api'
import { currentAccount } from '@/_lib/server/account'
import { getDictionary } from '../_i18n'
import { IAdminUser, ICompany } from '../model'
import { PageHeader } from '../_components/Page'
import CompanyFilter from '../_components/CompanyFilter'
import { UserTable } from './UserTable'

/** Super administrators see the employees of every company, administrators those of their own. */
export default async function UsersPage({ searchParams }: { searchParams: Promise<{ company?: string }> }) {
  const me = await currentAccount()
  if (!me.isAdmin) return null
  const { t } = await getDictionary()
  const all = await (await httpGet('admin/users')).json() as IAdminUser[]
  const companies = me.isSuperAdmin ? await (await httpGet('admin/companies')).json() as ICompany[] : []
  const company = me.isSuperAdmin ? (await searchParams).company ?? '' : ''
  const users = company ? all.filter(u => u.companyId === company) : all
  const newHref = `/admin/users/new${company ? `?company=${company}` : ''}`

  return (
    <div className="space-y-6">
      <PageHeader title={t.users} hint={!me.isSuperAdmin && me.companyName ? me.companyName : undefined}>
        {me.isSuperAdmin && <CompanyFilter companies={companies} value={company} allLabel={t.allCompanies} label={t.companyOf} />}
        <Link href={newHref} className="inline-flex items-center gap-x-1.5 rounded-md bg-primary px-3 py-2 text-sm font-semibold text-white shadow-xs hover:bg-primary-hover">
          <PlusIcon className="-ml-0.5 size-5" aria-hidden="true" />
          {t.newUser}
        </Link>
      </PageHeader>
      <UserTable users={users} t={t} showCompany={me.isSuperAdmin} />
    </div>
  )
}
