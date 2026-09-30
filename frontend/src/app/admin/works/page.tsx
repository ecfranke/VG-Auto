import Link from 'next/link'
import { redirect } from 'next/navigation'
import { httpGet } from '@/_lib/server/query-api'
import { currentAccount } from '@/_lib/server/account'
import { vehicleTitle } from '@/_lib/shared/vehicle'
import { getDictionary } from '../_i18n'
import { IAdminWorkRow, ICompany } from '../model'
import { PageHeader, Table } from '../_components/Page'
import { Alert } from '../_components/Fields'
import { Badge } from '../_components/Badges'

const PAGE = 50
const inputClass = 'rounded-md bg-surface px-3 py-1.5 text-sm text-gray-900 outline-1 -outline-offset-1 outline-gray-300 placeholder:text-gray-400 focus:outline-2 focus:-outline-offset-2 focus:outline-primary'

/** The work of every company (super administrators). */
export default async function WorksPage({ searchParams }: { searchParams: Promise<{ q?: string, company?: string, page?: string, deleted?: string }> }) {
  const me = await currentAccount()
  if (!me.isAdmin) return null
  if (!me.isSuperAdmin) redirect('/admin')
  const { t, lang } = await getDictionary()
  const query = await searchParams
  const q = (query.q ?? '').trim()
  const company = /^[0-9a-fA-F-]{36}$/.test(query.company ?? '') ? query.company! : ''
  const page = Math.max(0, parseInt(query.page ?? '0', 10) || 0)
  const params = new URLSearchParams({ limit: String(PAGE), offset: String(page * PAGE) })
  if (q) params.set('searchText', q)
  if (company) params.set('companyId', company)
  const data = await (await httpGet(`admin/works?${params}`)).json() as { items: IAdminWorkRow[], hasMore: boolean }
  const companies = await (await httpGet('admin/companies')).json() as ICompany[]
  const format = new Intl.DateTimeFormat(lang === 'zh' ? 'zh-CN' : 'en-GB', { dateStyle: 'medium' })
  const pageHref = (p: number) => {
    const next = new URLSearchParams()
    if (q) next.set('q', q)
    if (company) next.set('company', company)
    next.set('page', String(p))
    return `/admin/works?${next}`
  }

  return (
    <div className="space-y-6">
      <PageHeader title={t.works} hint={t.worksHint} />
      {query.deleted && <Alert kind="success">{t.workDeleted} {query.deleted}</Alert>}

      <form className="flex flex-wrap items-end gap-3">
        <input name="q" defaultValue={q} placeholder={t.searchWork} aria-label={t.search} className={`${inputClass} w-full sm:w-96`} />
        <select name="company" defaultValue={company} aria-label={t.companyOf} className={inputClass}>
          <option value="">{t.allCompanies}</option>
          {companies.map(c => <option key={c.id} value={c.id}>{c.name}</option>)}
        </select>
        <button type="submit" className="rounded-md bg-primary px-3 py-2 text-sm font-semibold text-white shadow-xs hover:bg-primary-hover">{t.search}</button>
      </form>

      <Table head={[t.workCol, t.companyOf, t.client, t.vehicle, t.started, t.estimates, t.invoice, t.status]} empty={data.items.length === 0} emptyText={t.empty}>
        {data.items.map(w => (
          <tr key={w.id} className="hover:bg-gray-50">
            <td className="px-4 py-3">
              <Link href={`/admin/works/${w.id}`} className="font-mono text-xs font-semibold whitespace-nowrap text-link hover:text-link-hover">{w.code}</Link>
            </td>
            <td className="px-4 py-3 text-gray-700">{w.companyName}</td>
            <td className="px-4 py-3 text-gray-700">{w.clientName || '—'}</td>
            <td className="px-4 py-3 text-gray-700">
              {vehicleTitle({ year: w.vehicleYear, manufacturer: w.vehicleManufacturer, model: w.vehicleModel }) || '—'}
              {w.licensePlate && <div className="text-xs text-gray-500">{w.licensePlate}</div>}
            </td>
            <td className="px-4 py-3 whitespace-nowrap text-gray-700">{format.format(new Date(w.startedOn))}</td>
            <td className="px-4 py-3">
              <span className="text-gray-700">{w.offers}</span>
              <span className="ml-2 inline-flex flex-wrap gap-1">
                {w.sentOffers > 0 && <Badge color="gray">{w.sentOffers} {t.sentCount}</Badge>}
                {w.signedOffers > 0 && <Badge color="green">{w.signedOffers} {t.signedCount}</Badge>}
              </span>
            </td>
            <td className="px-4 py-3">
              {w.invoiceNumber == null ? <span className="text-gray-400">—</span> : (
                <span className="flex flex-wrap items-center gap-1">
                  <span className="text-gray-700">#{w.invoiceNumber}</span>
                  {w.invoicePaid ? <Badge color="green">{t.paid}</Badge> : <Badge color="blue">{t.unpaid}</Badge>}
                </span>
              )}
            </td>
            <td className="px-4 py-3"><Badge color={w.status === 'completed' ? 'purple' : 'gray'}>{t.workStatus[w.status] ?? w.status}</Badge></td>
          </tr>
        ))}
      </Table>

      <div className="flex justify-end gap-x-2 text-sm">
        {page > 0 && <Link href={pageHref(page - 1)} className="rounded-md bg-surface px-3 py-1.5 text-gray-700 ring-1 ring-gray-300 hover:bg-gray-50">{t.previous}</Link>}
        {data.hasMore && <Link href={pageHref(page + 1)} className="rounded-md bg-surface px-3 py-1.5 text-gray-700 ring-1 ring-gray-300 hover:bg-gray-50">{t.next}</Link>}
      </div>
    </div>
  )
}
