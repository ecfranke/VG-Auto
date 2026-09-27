import Link from 'next/link'
import { httpGet } from '@/_lib/server/query-api'
import { currentAccount } from '@/_lib/server/account'
import { getDictionary } from '../_i18n'
import { IAuditEntry } from '../model'

const PAGE = 50

export default async function AuditPage({ searchParams }: { searchParams: Promise<{ page?: string }> }) {
  if (!(await currentAccount()).isAdmin) return null
  const { t, lang } = await getDictionary()
  const page = Math.max(0, parseInt((await searchParams).page ?? '0', 10) || 0)
  const data = await (await httpGet(`admin/audit?limit=${PAGE}&offset=${page * PAGE}`)).json() as { items: IAuditEntry[], total: number }
  const format = new Intl.DateTimeFormat(lang === 'zh' ? 'zh-CN' : 'en-GB', { dateStyle: 'medium', timeStyle: 'short' })
  const pages = Math.max(1, Math.ceil(data.total / PAGE))

  return (
    <div>
      <h1 className="text-xl font-semibold text-gray-900">{t.audit}</h1>
      <div className="mt-6 overflow-x-auto rounded-lg bg-white shadow-sm ring-1 ring-gray-900/5">
        <table className="min-w-full divide-y divide-gray-200 text-sm">
          <thead className="bg-gray-50 text-left text-gray-900">
            <tr>
              <th className="px-4 py-3 font-semibold">{t.when}</th>
              <th className="px-4 py-3 font-semibold">{t.actor}</th>
              <th className="px-4 py-3 font-semibold">{t.action}</th>
              <th className="px-4 py-3 font-semibold">{t.target}</th>
              <th className="px-4 py-3 font-semibold">{t.details}</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-gray-100">
            {data.items.map(entry => (
              <tr key={entry.id}>
                <td className="whitespace-nowrap px-4 py-3 text-gray-600">{format.format(new Date(entry.createdAt))}</td>
                <td className="px-4 py-3 text-gray-900">{entry.actor}</td>
                <td className="px-4 py-3 text-gray-900">{t.actions[entry.action] ?? entry.action}</td>
                <td className="px-4 py-3 text-gray-700">{entry.target ?? '—'}</td>
                <td className="px-4 py-3 text-gray-500">{entry.details}</td>
              </tr>
            ))}
            {data.items.length === 0 && <tr><td colSpan={5} className="px-4 py-6 text-center text-gray-500">{t.empty}</td></tr>}
          </tbody>
        </table>
      </div>
      <div className="mt-4 flex items-center justify-between text-sm text-gray-600">
        <span>{data.total} {t.total}</span>
        <div className="flex gap-x-2">
          {page > 0 && <Link href={`/admin/audit?page=${page - 1}`} className="rounded-md bg-white px-3 py-1.5 ring-1 ring-gray-300 hover:bg-gray-50">{t.previous}</Link>}
          {page + 1 < pages && <Link href={`/admin/audit?page=${page + 1}`} className="rounded-md bg-white px-3 py-1.5 ring-1 ring-gray-300 hover:bg-gray-50">{t.next}</Link>}
        </div>
      </div>
    </div>
  )
}
