import Link from 'next/link'
import moment from 'moment'
import { cookies } from 'next/headers'
import { jwtDecode } from 'jwt-decode'
import {
  ArrowRightIcon,
  ClipboardDocumentCheckIcon,
  ExclamationTriangleIcon,
  PlusIcon,
  ReceiptPercentIcon,
  CalendarDaysIcon,
  WrenchScrewdriverIcon,
} from '@heroicons/react/24/outline'
import { httpGet } from '@/_lib/server/query-api'
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

/** API: GET work/summary */
interface IWorkSummary {
  openWork: number
  offersAwaiting: number
  unpaidInvoices: number
  overdueInvoices: number
  startedThisMonth: number
}

const quickActions = [
  { href: '/home/work/new', label: 'New work', primary: true },
  { href: '/home/clients/new', label: 'New client' },
  { href: '/home/vehicles/new', label: 'New vehicle' },
]

/** Staggered fade-in of the blocks of the page (CSS only). */
const appear = (i: number) => ({ animationDelay: `${i * 70}ms` })

/** Home page: greeting, quick actions, the figures of the workshop and the most recent work. */
export default async function Page() {
  const jwt = (await cookies()).get('jwt')?.value
  const fullName = jwt ? jwtDecode<{ FullName?: string }>(jwt).FullName ?? '' : ''
  const firstName = fullName.split(' ')[0]
  const [page, summary] = await Promise.all([
    httpGet('work/page?limit=10&offset=0&scope=all').then(r => r.json() as Promise<{ items: IRecentWork[] }>),
    httpGet('work/summary').then(r => r.json() as Promise<IWorkSummary>),
  ])
  const recent = page.items ?? []

  const stats = [
    { label: 'Open work', value: summary.openWork, href: '/home/work?status=inprogress', icon: WrenchScrewdriverIcon, note: 'not invoiced yet' },
    { label: 'Estimates awaiting answer', value: summary.offersAwaiting, href: '/home/work', icon: ClipboardDocumentCheckIcon, note: 'sent, not accepted' },
    {
      label: 'Unpaid invoices', value: summary.unpaidInvoices, href: '/home/work?issued=on', icon: ReceiptPercentIcon,
      note: summary.overdueInvoices > 0 ? `${summary.overdueInvoices} overdue` : 'none overdue', warning: summary.overdueInvoices > 0,
      noteHref: summary.overdueInvoices > 0 ? '/home/work?issued=on&status=overdue' : undefined,
    },
    { label: 'Started this month', value: summary.startedThisMonth, href: '/home/work', icon: CalendarDaysIcon, note: moment().format('MMMM YYYY') },
  ]

  return (
    <main className="motion-steady pb-10 lg:pl-62">
      <div className="space-y-6 px-4 py-6 sm:px-6 lg:px-8">
        {/* greeting */}
        <section className="relative isolate overflow-hidden rounded-2xl bg-slate-950 px-6 py-8 shadow-sm ring-1 ring-white/10 sm:px-8 motion-safe:animate-fade-up">
          <div aria-hidden="true" className="pointer-events-none absolute inset-0 -z-10">
            <div className="absolute -top-24 -right-16 size-80 rounded-full bg-blue-600/30 blur-3xl motion-safe:animate-drift" />
            <div className="absolute -bottom-32 left-1/4 size-72 rounded-full bg-indigo-500/20 blur-3xl motion-safe:animate-drift-reverse" />
            <div className="absolute inset-y-0 left-0 w-1/4 bg-gradient-to-r from-transparent via-white/[0.04] to-transparent motion-safe:animate-shine" />
          </div>
          <div className="flex flex-wrap items-end justify-between gap-6">
            <div>
              <p className="text-sm text-blue-200">{moment().format('dddd, LL')}</p>
              <h1 className="mt-1 text-2xl font-semibold tracking-tight text-white sm:text-3xl">
                Welcome back{firstName ? `, ${firstName}` : ''}
              </h1>
              <p className="mt-2 text-sm text-slate-300">
                {summary.openWork === 0 ? 'No open work right now.' : `${summary.openWork} open ${summary.openWork === 1 ? 'job' : 'jobs'} in the workshop.`}
              </p>
            </div>
            <div className="flex flex-wrap gap-2">
              {quickActions.map(a => (
                <Link key={a.href} href={a.href}
                  className={a.primary
                    ? 'inline-flex items-center gap-x-1.5 rounded-lg bg-primary px-3.5 py-2 text-sm font-semibold text-white shadow-sm transition hover:bg-primary-hover'
                    : 'inline-flex items-center gap-x-1.5 rounded-lg bg-white/10 px-3.5 py-2 text-sm font-semibold text-white ring-1 ring-white/15 ring-inset transition hover:bg-white/15'}>
                  <PlusIcon className="-ml-0.5 size-4" aria-hidden="true" />
                  {a.label}
                </Link>
              ))}
            </div>
          </div>
        </section>

        {/* figures */}
        <dl className="grid grid-cols-2 gap-3 sm:gap-4 xl:grid-cols-4">
          {stats.map((s, i) => (
            <div key={s.label} style={appear(i + 1)}
              className="group relative rounded-xl bg-surface p-4 shadow-sm sm:p-5 ring-1 ring-gray-900/5 transition duration-300 hover:-translate-y-0.5 hover:shadow-md hover:ring-blue-500/30 motion-safe:animate-fade-up">
              <div className="flex items-start justify-between gap-3">
                <dt className="text-sm font-medium text-gray-500">
                  <Link href={s.href} className="after:absolute after:inset-0">{s.label}</Link>
                </dt>
                <span className="flex size-9 items-center justify-center rounded-lg bg-blue-50 text-link transition group-hover:bg-primary group-hover:text-white">
                  <s.icon className="size-5" aria-hidden="true" />
                </span>
              </div>
              <dd className="mt-2 text-3xl font-semibold tracking-tight text-gray-900 tabular-nums">{s.value}</dd>
              <dd className={`mt-1 flex items-center gap-1 text-xs ${s.warning ? 'text-yellow-700' : 'text-gray-500'}`}>
                {s.warning && <ExclamationTriangleIcon className="size-3.5" aria-hidden="true" />}
                {s.noteHref ? <Link href={s.noteHref} className="relative z-10 hover:underline">{s.note}</Link> : s.note}
              </dd>
            </div>
          ))}
        </dl>

        {/* recent work */}
        <section style={appear(5)} className="overflow-hidden rounded-xl bg-surface shadow-sm ring-1 ring-gray-900/5 motion-safe:animate-fade-up">
          <div className="flex flex-wrap items-center justify-between gap-3 border-b border-gray-100 px-5 py-4">
            <div>
              <h2 className="text-base font-semibold text-gray-900">Recent work</h2>
              <p className="mt-0.5 text-sm text-gray-500">The 10 most recently updated jobs, finished ones included.</p>
            </div>
            <Link href="/home/work" className="group inline-flex items-center gap-x-1 text-sm font-semibold text-link hover:text-link-hover">
              All work
              <ArrowRightIcon className="size-4 transition-transform group-hover:translate-x-0.5" aria-hidden="true" />
            </Link>
          </div>
          {recent.length === 0
            ? <div className="px-6 py-14 text-center">
                <WrenchScrewdriverIcon className="mx-auto size-10 text-gray-300" aria-hidden="true" />
                <p className="mt-3 text-sm text-gray-500">No work yet.</p>
                <Link href="/home/work/new" className="mt-4 inline-flex items-center gap-x-1.5 rounded-lg bg-primary px-3.5 py-2 text-sm font-semibold text-white hover:bg-primary-hover">
                  <PlusIcon className="size-4" aria-hidden="true" />
                  Start the first one
                </Link>
              </div>
            : <div className="overflow-x-auto">
                <table className="min-w-full divide-y divide-gray-200 text-sm">
                  <thead className="bg-gray-50 text-left text-gray-900">
                    <tr>
                      <th className="px-4 py-3 font-semibold sm:pl-5">Work</th>
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
                        <tr key={w.id} className={`transition-colors hover:bg-gray-50 ${w.status === 'closed' ? 'line-through' : ''}`}>
                          <td className="px-4 py-3 whitespace-nowrap sm:pl-5">
                            <Link href={`/home/work/${w.id}`} className="font-mono text-xs font-semibold text-link hover:text-link-hover">{w.code}</Link>
                          </td>
                          <td className="px-4 py-3">
                            <div className="flex gap-1">
                              {w.hasRepairs && <BlueBadge text="Repair job" />}
                              {w.numberOfOffers > 0 && <BlueBadge text={w.numberOfOffers > 1 ? 'Many offers' : 'Offer'} />}
                            </div>
                          </td>
                          <td className="px-4 py-3"><WorkStatusBadge status={w.status} /></td>
                          <td className="px-4 py-3">
                            {w.clientId ? <Link href={`/home/clients/${w.clientId}`} className="text-gray-900 hover:text-link">{w.clientName}</Link> : '—'}
                          </td>
                          <td className="px-4 py-3">
                            {w.vehicleId
                              ? <Link href={`/home/vehicles/${w.vehicleId}`} className="text-gray-900 hover:text-link">
                                  {w.licensePlate || title}
                                  {w.licensePlate && title && <span className="block text-xs text-gray-500">{title}</span>}
                                </Link>
                              : '—'}
                          </td>
                          <td className="px-4 py-3 text-gray-700">{w.mechanicNames || '—'}</td>
                          <td className="px-4 py-3 whitespace-nowrap text-gray-700">{moment(w.startedOn).format('LL')}</td>
                          <td className="px-4 py-3 text-gray-700"><p title={w.notes ?? ''} className="max-w-xs truncate">{w.notes}</p></td>
                        </tr>
                      )
                    })}
                  </tbody>
                </table>
              </div>}
        </section>
      </div>
    </main>
  )
}
