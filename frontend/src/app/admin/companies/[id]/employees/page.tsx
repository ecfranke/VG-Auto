import Link from 'next/link'
import { notFound } from 'next/navigation'
import { PlusIcon } from '@heroicons/react/20/solid'
import { httpGet } from '@/_lib/server/query-api'
import { currentAccount } from '@/_lib/server/account'
import { getDictionary } from '../../../_i18n'
import { IAdminUser } from '../../../model'
import { UserTable } from '../../../users/UserTable'

export default async function CompanyEmployeesPage({ params }: { params: Promise<{ id: string }> }) {
  const me = await currentAccount()
  if (!me.isAdmin) return null
  const { id } = await params
  if (!/^[0-9a-fA-F-]{36}$/.test(id)) notFound()
  const { t } = await getDictionary()
  const users = (await (await httpGet('admin/users')).json() as IAdminUser[]).filter(u => u.companyId === id)
  return (
    <div className="space-y-4">
      <div className="flex justify-end">
        <Link href={`/admin/users/new?company=${id}`} className="inline-flex items-center gap-x-1.5 rounded-md bg-primary px-3 py-2 text-sm font-semibold text-white shadow-xs hover:bg-primary-hover">
          <PlusIcon className="-ml-0.5 size-5" aria-hidden="true" />
          {t.addEmployee}
        </Link>
      </div>
      <UserTable users={users} t={t} showCompany={false} />
    </div>
  )
}
