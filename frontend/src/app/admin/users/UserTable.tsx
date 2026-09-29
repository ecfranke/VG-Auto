import Link from 'next/link'
import type { Dictionary } from '../_i18n'
import type { IAdminUser } from '../model'
import { RoleBadges, StatusBadges } from '../_components/Badges'
import { Table } from '../_components/Page'

/** Employees with a login first, then the others. */
export function UserTable({ users, t, showCompany }: { users: IAdminUser[], t: Dictionary, showCompany: boolean }) {
  const sorted = [...users.filter(u => u.hasAccount), ...users.filter(u => !u.hasAccount)]
  return (
    <Table head={[t.name, t.userName, ...(showCompany ? [t.companyOf] : []), t.email, t.role, t.status]} empty={users.length === 0} emptyText={t.empty}>
      {sorted.map(user => (
        <tr key={user.employeeId} className="hover:bg-gray-50">
          <td className="px-4 py-3">
            <Link href={`/admin/users/${user.employeeId}`} className="font-medium text-link hover:text-link-hover">
              {user.firstName} {user.lastName}
            </Link>
            {user.isSelf && <span className="ml-1 text-gray-400">({t.you})</span>}
            {user.profession && <div className="text-xs text-gray-500">{user.profession}</div>}
          </td>
          <td className="px-4 py-3 text-gray-700">{user.userName ?? '—'}</td>
          {showCompany && (
            <td className="px-4 py-3 text-gray-700">
              <Link href={`/admin/companies/${user.companyId}`} className="hover:text-link">{user.companyName ?? '—'}</Link>
            </td>
          )}
          <td className="px-4 py-3 text-gray-700">{user.email ?? '—'}</td>
          <td className="px-4 py-3"><RoleBadges user={user} t={t} /></td>
          <td className="px-4 py-3"><StatusBadges user={user} t={t} /></td>
        </tr>
      ))}
    </Table>
  )
}
