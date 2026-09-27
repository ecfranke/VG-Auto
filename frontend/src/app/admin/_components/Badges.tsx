import clsx from 'clsx'
import { Dictionary } from '../_i18n'
import { IAdminUser } from '../model'

export function Badge({ children, color }: { children: React.ReactNode, color: 'gray' | 'green' | 'red' | 'yellow' | 'indigo' | 'purple' | 'blue' }) {
  const colors = {
    gray: 'bg-gray-50 text-gray-600 ring-gray-500/10',
    green: 'bg-green-50 text-green-700 ring-green-600/20',
    red: 'bg-red-50 text-red-700 ring-red-600/10',
    yellow: 'bg-yellow-50 text-yellow-800 ring-yellow-600/20',
    indigo: 'bg-indigo-50 text-indigo-700 ring-indigo-700/10',
    purple: 'bg-purple-50 text-purple-700 ring-purple-700/10',
    blue: 'bg-blue-50 text-blue-700 ring-blue-700/10',
  }
  return <span className={clsx('inline-flex items-center rounded-md px-2 py-0.5 text-xs font-medium ring-1 ring-inset whitespace-nowrap', colors[color])}>{children}</span>
}

export function RoleBadges({ user, t }: { user: IAdminUser, t: Dictionary }) {
  if (!user.hasAccount) return <Badge color="gray">{t.noLogin}</Badge>
  return (
    <span className="flex flex-wrap gap-1">
      <Badge color={user.role === 'superadmin' ? 'purple' : user.role === 'admin' ? 'indigo' : 'gray'}>{t.roles[user.role ?? 'user']}</Badge>
      {user.isOwner && <Badge color="purple">{t.owner}</Badge>}
    </span>
  )
}

export function StatusBadges({ user, t }: { user: IAdminUser, t: Dictionary }) {
  if (!user.hasAccount) return null
  return (
    <span className="flex flex-wrap gap-1">
      {user.disabled ? <Badge color="red">{t.disabled}</Badge> : <Badge color="green">{t.active}</Badge>}
      {user.locked && <Badge color="yellow">{t.locked}</Badge>}
      {user.mustChangePassword && <Badge color="blue">{t.mustChangePassword}</Badge>}
      {user.microsoftLinked && <Badge color="gray">{t.microsoft}</Badge>}
    </span>
  )
}
