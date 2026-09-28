import Link from 'next/link'
import { PlusIcon } from '@heroicons/react/20/solid'
import { httpGet } from '@/_lib/server/query-api'
import { currentAccount } from '@/_lib/server/account'
import { getDictionary } from '../_i18n'
import { IAdminUser } from '../model'
import { RoleBadges, StatusBadges } from '../_components/Badges'

export default async function UsersPage() {
  if (!(await currentAccount()).isAdmin) return null
  const { t } = await getDictionary()
  const users = await (await httpGet('admin/users')).json() as IAdminUser[]
  const accounts = users.filter(u => u.hasAccount)
  const others = users.filter(u => !u.hasAccount)

  return (
    <div>
      <div className="flex items-center justify-between">
        <h1 className="text-xl font-semibold text-gray-900">{t.users}</h1>
        <Link href="/admin/users/new" className="inline-flex items-center gap-x-1.5 rounded-md bg-indigo-600 px-3 py-2 text-sm font-semibold text-white shadow-xs hover:bg-indigo-500">
          <PlusIcon className="-ml-0.5 size-5" aria-hidden="true" />
          {t.newUser}
        </Link>
      </div>

      <div className="mt-6 overflow-x-auto rounded-lg bg-white shadow-sm ring-1 ring-gray-900/5">
        <table className="min-w-full divide-y divide-gray-200 text-sm">
          <thead className="bg-gray-50 text-left text-gray-900">
            <tr>
              <th className="px-4 py-3 font-semibold">{t.name}</th>
              <th className="px-4 py-3 font-semibold">{t.userName}</th>
              <th className="px-4 py-3 font-semibold">{t.companyOf}</th>
              <th className="px-4 py-3 font-semibold">{t.email}</th>
              <th className="px-4 py-3 font-semibold">{t.role}</th>
              <th className="px-4 py-3 font-semibold">{t.status}</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-gray-100">
            {[...accounts, ...others].map(user => (
              <tr key={user.employeeId} className="hover:bg-gray-50">
                <td className="px-4 py-3">
                  <Link href={`/admin/users/${user.employeeId}`} className="font-medium text-indigo-600 hover:text-indigo-500">
                    {user.firstName} {user.lastName}
                  </Link>
                  {user.isSelf && <span className="ml-1 text-gray-400">({t.you})</span>}
                  {user.profession && <div className="text-xs text-gray-500">{user.profession}</div>}
                </td>
                <td className="px-4 py-3 text-gray-700">{user.userName ?? '—'}</td>
                <td className="px-4 py-3 text-gray-700">{user.companyName ?? '—'}</td>
                <td className="px-4 py-3 text-gray-700">{user.email ?? '—'}</td>
                <td className="px-4 py-3"><RoleBadges user={user} t={t} /></td>
                <td className="px-4 py-3"><StatusBadges user={user} t={t} /></td>
              </tr>
            ))}
            {users.length === 0 && <tr><td colSpan={6} className="px-4 py-6 text-center text-gray-500">{t.empty}</td></tr>}
          </tbody>
        </table>
      </div>
    </div>
  )
}
