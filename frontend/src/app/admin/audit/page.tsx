import Link from 'next/link'
import { httpGet } from '@/_lib/server/query-api'
import { currentAccount } from '@/_lib/server/account'
import { getDictionary } from '../_i18n'
import { IAuditEntry, ICompany } from '../model'
import { PageHeader, Table } from '../_components/Page'
import CompanyFilter from '../_components/CompanyFilter'

const PAGE = 50

/** Super administrators see every entry (optionally of one company), administrators those of their company. */
export default async function AuditPage({ searchParams }: { searchParams: Promise<{ page?: string, company?: string }> }) {
  const me = await currentAccount()
  if (!me.isAdmin) return null
  const { t, lang } = await getDictionary()
  const query = await searchParams
  const page = Math.max(0, parseInt(query.page ?? '0', 10) || 0)
  const company = me.isSuperAdmin && /^[0-9a-fA-F-]{36}$/.test(query.company ?? '') ? query.company! : ''
  const data = await (await httpGet(`admin/audit?limit=${PAGE}&offset=${page * PAGE}${company ? `&companyId=${company}` : ''}`)).json() as { items: IAuditEntry[], total: number }
  const companies = me.isSuperAdmin ? await (await httpGet('admin/companies')).json() as ICompany[] : []
  const names = new Map(companies.map(c => [c.id, c.name]))
  const format = new Intl.DateTimeFormat(lang === 'zh' ? 'zh-CN' : 'en-GB', { dateStyle: 'medium', timeStyle: 'short' })
  const pages = Math.max(1, Math.ceil(data.total / PAGE))
  const pageHref = (p: number) => `/admin/audit?page=${p}${company ? `&company=${company}` : ''}`

  return (
    <div className="space-y-6">
      <PageHeader title={t.audit} hint={!me.isSuperAdmin && me.companyName ? me.companyName : undefined}>
        {me.isSuperAdmin && <CompanyFilter companies={companies} value={company} allLabel={t.allCompanies} label={t.companyOf} />}
      </PageHeader>
      <Table head={[t.when, t.actor, t.action, t.target, ...(me.isSuperAdmin ? [t.companyOf] : []), t.details]}
        empty={data.items.length === 0} emptyText={t.empty}>
        {data.items.map(entry => (
          <tr key={entry.id}>
            <td className="whitespace-nowrap px-4 py-3 text-gray-600">{format.format(new Date(entry.createdAt))}</td>
            <td className="px-4 py-3 text-gray-900">{entry.actor}</td>
            <td className="px-4 py-3 text-gray-900">{t.actions[entry.action] ?? entry.action}</td>
            <td className="px-4 py-3 text-gray-700">{entry.target ?? '—'}</td>
            {me.isSuperAdmin && (
              <td className="px-4 py-3 text-gray-700">
                {entry.companyId ? names.get(entry.companyId) ?? '—' : <span className="text-gray-400">{t.system}</span>}
              </td>
            )}
            <td className="px-4 py-3 text-gray-500">{entry.details}</td>
          </tr>
        ))}
      </Table>
      <div className="flex items-center justify-between text-sm text-gray-600">
        <span>{data.total} {t.total}</span>
        <div className="flex gap-x-2">
          {page > 0 && <Link href={pageHref(page - 1)} className="rounded-md bg-surface px-3 py-1.5 ring-1 ring-gray-300 hover:bg-gray-50">{t.previous}</Link>}
          {page + 1 < pages && <Link href={pageHref(page + 1)} className="rounded-md bg-surface px-3 py-1.5 ring-1 ring-gray-300 hover:bg-gray-50">{t.next}</Link>}
        </div>
      </div>
    </div>
  )
}
