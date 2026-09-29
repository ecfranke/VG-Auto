import { httpGet } from '@/_lib/server/query-api'
import { currentAccount } from '@/_lib/server/account'
import { ICompany } from '../../model'
import { getDictionary } from '../../_i18n'
import { Card, PageHeader } from '../../_components/Page'
import NewUserForm from './NewUserForm'

/** Super administrators add employees to any company, administrators to their own. */
export default async function NewUserPage({ searchParams }: { searchParams: Promise<{ company?: string }> }) {
  const me = await currentAccount()
  if (!me.isAdmin) return null
  const { t } = await getDictionary()
  const companies = me.isSuperAdmin
    ? await (await httpGet('admin/companies')).json() as ICompany[]
    : [{ id: me.companyId, name: me.companyName ?? '' }]
  const wanted = (await searchParams).company
  const defaultCompany = companies.some(c => c.id === wanted) ? wanted! : me.companyId
  const roles = me.isSuperAdmin ? ['user', 'admin', 'superadmin'] : ['user']
  return (
    <div className="space-y-6">
      <PageHeader title={t.newUser} back={{ href: '/admin/users', label: t.users }} />
      <Card>
        <NewUserForm t={t} roles={roles} companies={companies} defaultCompany={defaultCompany} canChooseCompany={me.isSuperAdmin} />
      </Card>
    </div>
  )
}
