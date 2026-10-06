import { httpGet } from '@/_lib/server/query-api'
import { currentAccount } from '@/_lib/server/account'
import { vehicleTitle } from '@/_lib/shared/vehicle'
import { getDictionary } from '../_i18n'
import { IAdminVehicleRow, ICompany } from '../model'
import { PageHeader, Table } from '../_components/Page'
import { Alert } from '../_components/Fields'
import DeleteRowButton from '../_components/DeleteRowButton'
import { ListSearch, Pager, listParams } from '../_components/ListControls'

const PAGE = 50

/** Vehicles: of every company for super administrators, of the own company for administrators. */
export default async function VehiclesPage({ searchParams }: { searchParams: Promise<{ q?: string, company?: string, page?: string, deleted?: string }> }) {
  const me = await currentAccount()
  if (!me.isAdmin) return null
  const { t } = await getDictionary()
  const query = await searchParams
  const list = listParams('/admin/vehicles', query, me.isSuperAdmin)
  const data = await (await httpGet(`admin/vehicles?${list.api(PAGE)}`)).json() as { items: IAdminVehicleRow[], hasMore: boolean }
  const companies = me.isSuperAdmin ? await (await httpGet('admin/companies')).json() as ICompany[] : null
  const head = [t.licensePlate, t.vehicle, t.vin, t.owner, ...(me.isSuperAdmin ? [t.companyOf] : []), { label: t.workOrders, right: true }, '']

  return (
    <div className="space-y-6">
      <PageHeader title={t.vehicles} hint={me.isSuperAdmin ? t.vehiclesHintAll : t.vehiclesHint} />
      {query.deleted && <Alert kind="success">{t.vehicleDeleted} {query.deleted}</Alert>}
      <ListSearch q={list.q} company={list.company} companies={companies} placeholder={t.searchVehicles} searchLabel={t.search}
        companyLabel={t.companyOf} allCompaniesLabel={t.allCompanies} />

      <Table head={head} empty={data.items.length === 0} emptyText={t.empty}>
        {data.items.map(v => {
          const title = vehicleTitle({ year: v.year, manufacturer: v.manufacturer, model: v.model })
          return (
            <tr key={v.id} className="hover:bg-gray-50">
              <td className="px-4 py-3 font-medium whitespace-nowrap text-gray-900">{v.licensePlate || '—'}</td>
              <td className="px-4 py-3 text-gray-700">{title || '—'}</td>
              <td className="px-4 py-3 font-mono text-xs text-gray-700">{v.vin || '—'}</td>
              <td className="px-4 py-3 text-gray-700">{v.owner || '—'}</td>
              {me.isSuperAdmin && <td className="px-4 py-3 text-gray-700">{v.companyName}</td>}
              <td className="px-4 py-3 text-right text-gray-700">{v.workCount}</td>
              <td className="px-4 py-3 text-right">
                <DeleteRowButton kind="vehicles" id={v.id} label={t.deleteItem} itemLabel={[v.licensePlate, title].filter(Boolean).join(' · ')}
                  confirmText={t.confirmDeleteVehicle} returnTo={list.href(list.page)} inUse={v.workCount > 0} inUseText={t.usedByData} />
              </td>
            </tr>
          )
        })}
      </Table>
      <Pager page={list.page} hasMore={data.hasMore} href={list.href} previous={t.previous} next={t.next} />
    </div>
  )
}
