import { httpGet } from '@/_lib/server/query-api'
import { currentAccount } from '@/_lib/server/account'
import { getDictionary } from '../_i18n'
import { IAdminClientRow, ICompany } from '../model'
import { PageHeader, Table } from '../_components/Page'
import { Alert } from '../_components/Fields'
import DeleteRowButton from '../_components/DeleteRowButton'
import { ListSearch, Pager, listParams } from '../_components/ListControls'

const PAGE = 50

/** Clients: of every company for super administrators, of the own company for administrators. */
export default async function ClientsPage({ searchParams }: { searchParams: Promise<{ q?: string, company?: string, page?: string, deleted?: string }> }) {
  const me = await currentAccount()
  if (!me.isAdmin) return null
  const { t } = await getDictionary()
  const query = await searchParams
  const list = listParams('/admin/clients', query, me.isSuperAdmin)
  const data = await (await httpGet(`admin/clients?${list.api(PAGE)}`)).json() as { items: IAdminClientRow[], hasMore: boolean }
  const companies = me.isSuperAdmin ? await (await httpGet('admin/companies')).json() as ICompany[] : null
  const head = [t.name, t.phone, t.email, ...(me.isSuperAdmin ? [t.companyOf] : []), { label: t.workOrders, right: true }, { label: t.vehicles, right: true }, '']

  return (
    <div className="space-y-6">
      <PageHeader title={t.clients} hint={me.isSuperAdmin ? t.clientsHintAll : t.clientsHint} />
      {query.deleted && <Alert kind="success">{t.clientDeleted} {query.deleted}</Alert>}
      <ListSearch q={list.q} company={list.company} companies={companies} placeholder={t.searchClients} searchLabel={t.search}
        companyLabel={t.companyOf} allCompaniesLabel={t.allCompanies} />

      <Table head={head} empty={data.items.length === 0} emptyText={t.empty}>
        {data.items.map(c => (
          <tr key={c.id} className="hover:bg-gray-50">
            <td className="px-4 py-3 font-medium text-gray-900">
              {c.name || '—'}
              {c.address && <div className="text-xs font-normal text-gray-500">{c.address}</div>}
            </td>
            <td className="px-4 py-3 text-gray-700">{c.phone || '—'}</td>
            <td className="px-4 py-3 text-gray-700">{c.email || '—'}</td>
            {me.isSuperAdmin && <td className="px-4 py-3 text-gray-700">{c.companyName}</td>}
            <td className="px-4 py-3 text-right text-gray-700">{c.workCount}</td>
            <td className="px-4 py-3 text-right text-gray-700">{c.vehicleCount}</td>
            <td className="px-4 py-3 text-right">
              <DeleteRowButton kind="clients" id={c.id} label={t.deleteItem} itemLabel={c.name} confirmText={t.confirmDeleteClient}
                returnTo={list.href(list.page)} inUse={c.workCount > 0 || c.vehicleCount > 0} inUseText={t.usedByData} />
            </td>
          </tr>
        ))}
      </Table>
      <Pager page={list.page} hasMore={data.hasMore} href={list.href} previous={t.previous} next={t.next} />
    </div>
  )
}
