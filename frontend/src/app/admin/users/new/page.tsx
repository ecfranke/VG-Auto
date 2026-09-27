import { currentAccount } from '@/_lib/server/account'
import { getDictionary } from '../../_i18n'
import NewUserForm from './NewUserForm'

export default async function NewUserPage() {
  const me = await currentAccount()
  if (!me.isAdmin) return null
  const { t } = await getDictionary()
  const roles = me.role === 'superadmin' ? ['user', 'admin', 'superadmin'] : ['user']
  return (
    <div className="rounded-lg bg-white p-6 shadow-sm ring-1 ring-gray-900/5">
      <h1 className="mb-6 text-xl font-semibold text-gray-900">{t.newUser}</h1>
      <NewUserForm t={t} roles={roles} />
    </div>
  )
}
