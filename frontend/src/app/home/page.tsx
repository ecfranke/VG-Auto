import Link from 'next/link'
import moment from 'moment'
import { cookies } from 'next/headers'
import { jwtDecode } from 'jwt-decode'
import { PlusIcon } from '@heroicons/react/20/solid'
import { httpGet } from '@/_lib/server/query-api'
import { Card, CardHeader } from '@/_components/Card'
import BlueBadge from '@/_components/BlueBadge'
import { vehicleTitle } from '@/_lib/shared/vehicle'
import WorkStatusBadge from './work/_components/activity/badges/WorkStatusBadge'

interface IRecentWork {
  id: string
  workNr: string
  code: string
  startedOn: string
  status: string
  clientId: string | null
  clientName: string | null
  vehicleId: string | null
  licensePlate: string | null
  vehicleManufacturer?: string | null
  vehicleModel?: string | null
  vehicleYear?: number | null
  mechanicNames: string | null
  notes: string | null
  hasRepairs: boolean
  numberOfOffers: number
}

const quickActions = [
  { href: '/home/work/new', label: 'New work' },
  { href: '/home/clients/new', label: 'New client' },
  { href: '/home/vehicles/new', label: 'New vehicle' },
]

/** Home page: quick actions and the most recent work. */
export default async function Page() {
  const jwt = (await cookies()).get('jwt')?.value
  const fullName = jwt ? jwtDecode<{ FullName?: string }>(jwt).FullName ?? '' : ''
  const page = await (await httpGet('work/page?limit=10&offset=0&scope=all')).json() as { items: IRecentWork[] }
  const recent = page.items ?? []

  return (
    <main className="lg:pl-62 pb-8">
      <div className="space-y-6 px-4 sm:px-6 sm:py-10 lg:px-8 lg:py-6">
        <div className="flex flex-wrap items-center justify-between gap-4">
          <div>
            <h1 className="text-xl font-semibold text-gray-900">Welcome{fullName ? `, ${fullName}` : ''}</h1>
            <p className="mt-1 text-sm text-gray-500">{moment().format('dddd, LL')}</p>
          </div>
          <div className="flex flex-wrap gap-2">
            {quickActions.map(a => (
              <Link key={a.href} href={a.href}
                className="inline-flex items-center gap-x-1.5 rounded-md bg-white px-3 py-2 text-sm font-semibold text-gray-900 shadow-xs ring-1 ring-gray-300 ring-inset hover:bg-gray-50">
                <PlusIcon className="-ml-0.5 size-5 text-gray-400" aria-hidden="true" />
                {a.label}
              </Link>
            ))}
          </div>
        </div>

        <Card header={
          <CardHeader title="Recent work" description="The 10 most recently updated jobs, finished ones included.">
            <Link href="/home/work" className="text-sm font-semibold text-indigo-600 hover:text-indigo-500">All work →</Link>
          </CardHeader>}>
          {recent.length === 0
            ? <p className="px-6 py-8 text-center text-sm text-gray-500">No work yet. <Link href="/home/work/new" className="font-semibold text-indigo-600">Start the first one</Link>.</p>
            : <div className="overflow-x-auto">
                <table className="min-w-full divide-y divide-gray-200 text-sm">
                  <thead className="text-left text-gray-900">
                    <tr>
                      <th className="px-4 py-3 font-semibold sm:pl-6">Work</th>
                      <th className="px-4 py-3 font-semibold">Type</th>
                      <th className="px-4 py-3 font-semibold">Status</th>
                      <th className="px-4 py-3 font-semibold">Client</th>
                      <th className="px-4 py-3 font-semibold">Vehicle</th>
                      <th className="px-4 py-3 font-semibold">Mechanics</th>
                      <th className="px-4 py-3 font-semibold">Start date</th>
                      <th className="px-4 py-3 font-semibold">Note</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-gray-100">
                    {recent.map(w => {
                      const title = vehicleTitle({ year: w.vehicleYear, manufacturer: w.vehicleManufacturer, model: w.vehicleModel })
                      return (
                        <tr key={w.id} className={w.status === 'closed' ? 'line-through' : ''}>
                          <td className="whitespace-nowrap px-4 py-3 sm:pl-6">
                            <Link href={`/home/work/${w.id}`} className="font-mono text-sm font-medium text-indigo-600 hover:text-indigo-500">{w.code}</Link>
                          </td>
                          <td className="px-4 py-3">
                            <div className="flex gap-1">
                              {w.hasRepairs && <BlueBadge text="Repair job" />}
                              {w.numberOfOffers > 0 && <BlueBadge text={w.numberOfOffers > 1 ? 'Many offers' : 'Offer'} />}
                            </div>
                          </td>
                          <td className="px-4 py-3"><WorkStatusBadge status={w.status} /></td>
                          <td className="px-4 py-3">
                            {w.clientId ? <Link href={`/home/clients/${w.clientId}`} className="text-gray-900 hover:text-indigo-600">{w.clientName}</Link> : '—'}
                          </td>
                          <td className="px-4 py-3">
                            {w.vehicleId
                              ? <Link href={`/home/vehicles/${w.vehicleId}`} className="text-gray-900 hover:text-indigo-600">
                                  {w.licensePlate || title}
                                  {w.licensePlate && title && <span className="block text-xs text-gray-500">{title}</span>}
                                </Link>
                              : '—'}
                          </td>
                          <td className="px-4 py-3 text-gray-700">{w.mechanicNames || '—'}</td>
                          <td className="whitespace-nowrap px-4 py-3 text-gray-700">{moment(w.startedOn).format('LL')}</td>
                          <td className="px-4 py-3 text-gray-700"><p title={w.notes ?? ''} className="max-w-xs truncate">{w.notes}</p></td>
                        </tr>
                      )
                    })}
                  </tbody>
                </table>
              </div>}
        </Card>
      </div>
    </main>
  )
}
