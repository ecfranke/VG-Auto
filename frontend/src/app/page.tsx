// VG Auto landing page: static (no data, no client code), the motion is CSS only
import Link from 'next/link'
import Image from 'next/image'
import {
  ArchiveBoxIcon,
  ArrowRightIcon,
  BuildingOffice2Icon,
  CheckBadgeIcon,
  DocumentTextIcon,
  EnvelopeIcon,
  PencilSquareIcon,
  ReceiptPercentIcon,
  TruckIcon,
  WrenchScrewdriverIcon,
} from '@heroicons/react/24/outline'
import { Container } from '@/_components/layout/Container'

const features = [
  { icon: WrenchScrewdriverIcon, title: 'Work orders', text: 'Estimates, repair jobs, parts and labour in one place. An accepted estimate becomes a repair job in one click.' },
  { icon: PencilSquareIcon, title: 'Online estimate signing', text: 'Clients open the estimate from their email, review every line and sign it on their phone.' },
  { icon: EnvelopeIcon, title: 'PDF estimates & invoices', text: 'Professional PDFs sent straight to the client by email, named after the job so they are easy to find.' },
  { icon: TruckIcon, title: 'Clients & vehicles', text: 'The full history of every customer and car: estimates, invoices and the work done, a search away.' },
  { icon: ArchiveBoxIcon, title: 'Inventory', text: 'Spare parts with storage locations, prices and discounts, added to a job straight from the shelf.' },
  { icon: ReceiptPercentIcon, title: 'Taxes done right', text: 'GST, HST, PST or QST by province, prices before tax and every tax shown on the documents.' },
]

const steps = [
  { icon: DocumentTextIcon, title: 'Estimate', text: 'Build it from parts and labour, send it as a PDF.' },
  { icon: CheckBadgeIcon, title: 'Signed', text: 'The client approves online — no printing, no calls.' },
  { icon: WrenchScrewdriverIcon, title: 'Repair', text: 'The job lists what to do, the mechanics and the parts.' },
  { icon: ReceiptPercentIcon, title: 'Invoice', text: 'One click turns the job into an invoice, sent by email.' },
]

const highlights = ['Online signing', 'GST / HST / PST', 'Several companies', 'Light & dark mode']

/** The animated preview of a job in the hero: its status moves from estimate to invoice. */
function JobPreview() {
  const statuses = [
    { label: 'Estimate sent', className: 'bg-blue-500/15 text-blue-300 ring-blue-400/30' },
    { label: 'Signed by client', className: 'bg-emerald-500/15 text-emerald-300 ring-emerald-400/30' },
    { label: 'In repair', className: 'bg-amber-500/15 text-amber-200 ring-amber-400/30' },
    { label: 'Invoiced', className: 'bg-violet-500/15 text-violet-200 ring-violet-400/30' },
  ]
  const lines = [
    { name: 'Brake pads, front axle', qty: '1 set', price: '$148.00' },
    { name: 'Brake disc 300 mm', qty: '2 pcs', price: '$212.00' },
    { name: 'Labour', qty: '1.5 h', price: '$165.00' },
  ]
  return (
    <div className="relative mx-auto w-full min-w-0 max-w-md motion-safe:animate-fade-up motion-safe:[animation-delay:350ms]">
      <div aria-hidden="true" className="absolute -inset-4 rounded-[2rem] bg-gradient-to-br from-blue-500/30 via-indigo-500/10 to-transparent blur-2xl" />
      <div className="relative overflow-hidden rounded-2xl bg-slate-900/80 p-5 shadow-2xl ring-1 ring-white/10 backdrop-blur">
        {/* a soft light passing over the card */}
        <div aria-hidden="true" className="pointer-events-none absolute inset-y-0 left-0 w-1/3 bg-gradient-to-r from-transparent via-white/[0.06] to-transparent motion-safe:animate-shine" />
        <div className="flex items-start justify-between gap-4">
          <div className="min-w-0">
            <p className="truncate font-mono text-xs font-semibold tracking-wide text-blue-300">RP_TF_2019_HC_2026_10_16_15</p>
            <p className="mt-1 text-sm font-semibold text-white">2019 Honda Civic · ABC 123</p>
            <p className="text-xs text-slate-400">Taylor Fischer</p>
          </div>
          <div className="h-6 shrink-0 overflow-hidden" aria-label="Status of the job">
            <div className="flex flex-col motion-safe:animate-status-cycle">
              {statuses.map(s => (
                <span key={s.label} className={`inline-flex h-6 items-center gap-1.5 rounded-full px-2.5 text-xs font-medium whitespace-nowrap ring-1 ring-inset ${s.className}`}>
                  <span className="size-1.5 rounded-full bg-current motion-safe:animate-pulse-dot" />
                  {s.label}
                </span>
              ))}
            </div>
          </div>
        </div>

        <div className="mt-4 h-1 overflow-hidden rounded-full bg-white/10">
          <div className="h-full origin-left scale-x-25 rounded-full bg-gradient-to-r from-blue-500 to-indigo-400 motion-safe:animate-progress-cycle" />
        </div>

        <ul className="mt-4 divide-y divide-white/5 text-sm">
          {lines.map((l, i) => (
            <li key={l.name} className="flex items-center justify-between gap-3 py-2.5 text-sm motion-safe:animate-fade-up"
              style={{ animationDelay: `${600 + i * 140}ms` }}>
              <span className="text-slate-200">{l.name}</span>
              <span className="flex items-center gap-4">
                <span className="text-xs text-slate-500">{l.qty}</span>
                <span className="w-20 text-right font-medium text-white tabular-nums">{l.price}</span>
              </span>
            </li>
          ))}
        </ul>

        <div className="mt-2 space-y-1 border-t border-white/10 pt-3 text-sm">
          <div className="flex justify-between text-slate-400"><span>Subtotal</span><span className="tabular-nums">$525.00</span></div>
          <div className="flex justify-between text-slate-400"><span>GST 5% · PST 7%</span><span className="tabular-nums">$63.00</span></div>
          <div className="flex justify-between pt-1 text-base font-semibold text-white"><span>Total</span><span className="tabular-nums">$588.00 CAD</span></div>
        </div>
      </div>
    </div>
  )
}

export default function Home() {
  return (
    <div className="motion-steady min-h-full bg-gray-50">
      {/* hero: always dark, like the menus of the app */}
      <section className="relative isolate overflow-hidden bg-slate-950">
        <div aria-hidden="true" className="pointer-events-none absolute inset-0 -z-10">
          <div className="absolute -top-40 -left-32 size-[34rem] rounded-full bg-blue-600/25 blur-3xl motion-safe:animate-drift" />
          <div className="absolute top-1/3 -right-40 size-[30rem] rounded-full bg-indigo-500/20 blur-3xl motion-safe:animate-drift-reverse" />
          <div className="absolute inset-0 bg-[linear-gradient(to_right,rgb(255_255_255/0.04)_1px,transparent_1px),linear-gradient(to_bottom,rgb(255_255_255/0.04)_1px,transparent_1px)] bg-[size:56px_56px] [mask-image:radial-gradient(ellipse_at_top,black_30%,transparent_75%)]" />
        </div>

        <Container>
          <header className="flex items-center justify-between py-6 motion-safe:animate-fade-in">
            <Link href="/" aria-label="VG Auto" className="flex items-center gap-x-3">
              <Image alt="" src="/logo.png" width={40} height={40} className="size-9" priority />
              <span className="text-lg font-semibold tracking-tight text-white">VG Auto</span>
            </Link>
            <Link href="/auth/login"
              className="rounded-full px-4 py-2 text-sm font-semibold text-white ring-1 ring-white/20 ring-inset transition hover:bg-white/10">
              Sign in
            </Link>
          </header>

          <div className="grid grid-cols-1 items-center gap-x-12 gap-y-16 pt-10 pb-24 lg:grid-cols-2 lg:pt-16 lg:pb-32">
            <div className="min-w-0">
              <p className="inline-flex items-center gap-x-2 rounded-full bg-white/5 px-3 py-1 text-xs font-medium text-blue-200 ring-1 ring-white/10 ring-inset motion-safe:animate-fade-up">
                <span className="size-1.5 rounded-full bg-blue-400 motion-safe:animate-pulse-dot" />
                Workshop management for repair shops
              </p>
              <h1 className="mt-6 text-4xl font-semibold tracking-tight text-balance text-white sm:text-5xl lg:text-6xl motion-safe:animate-fade-up motion-safe:[animation-delay:80ms]">
                From estimate to invoice,{' '}
                <span className="bg-gradient-to-r from-blue-400 to-indigo-300 bg-clip-text text-transparent">without the paperwork.</span>
              </h1>
              <p className="mt-6 max-w-xl text-lg text-pretty text-slate-300 motion-safe:animate-fade-up motion-safe:[animation-delay:160ms]">
                VG Auto keeps your work orders, clients, vehicles, parts and invoices in one clean place —
                so your team spends the day on cars, not on forms.
              </p>
              <div className="mt-10 flex flex-wrap items-center gap-4 motion-safe:animate-fade-up motion-safe:[animation-delay:240ms]">
                <Link href="/auth/login"
                  className="group inline-flex items-center gap-x-2 rounded-full bg-primary px-6 py-3 text-sm font-semibold text-white shadow-lg shadow-blue-600/30 transition hover:bg-primary-hover">
                  Sign in
                  <ArrowRightIcon className="size-4 transition-transform group-hover:translate-x-0.5" aria-hidden="true" />
                </Link>
                <a href="#how" className="rounded-full px-5 py-3 text-sm font-semibold text-slate-200 transition hover:text-white">
                  How it works
                </a>
              </div>
              <ul className="mt-10 flex flex-wrap gap-2 motion-safe:animate-fade-up motion-safe:[animation-delay:320ms]">
                {highlights.map(h => (
                  <li key={h} className="rounded-md bg-white/5 px-2.5 py-1 text-xs text-slate-300 ring-1 ring-white/10 ring-inset">{h}</li>
                ))}
              </ul>
            </div>
            <JobPreview />
          </div>
        </Container>
      </section>

      {/* features */}
      <section className="py-24 sm:py-28">
        <Container>
          <div className="mx-auto max-w-2xl text-center reveal">
            <h2 className="text-base font-semibold text-link">Everything in one place</h2>
            <p className="mt-2 text-3xl font-semibold tracking-tight text-balance text-gray-900 sm:text-4xl">
              Built around how a workshop really works
            </p>
          </div>
          <div className="mx-auto mt-14 grid max-w-5xl grid-cols-1 gap-5 sm:grid-cols-2 lg:grid-cols-3">
            {features.map(f => (
              <div key={f.title}
                className="group reveal rounded-2xl bg-surface p-6 shadow-sm ring-1 ring-gray-900/5 transition duration-300 hover:-translate-y-1 hover:shadow-lg hover:ring-blue-500/30">
                <div className="flex size-11 items-center justify-center rounded-xl bg-blue-50 text-link ring-1 ring-blue-600/10 transition group-hover:bg-primary group-hover:text-white">
                  <f.icon className="size-6" aria-hidden="true" />
                </div>
                <h3 className="mt-5 text-base font-semibold text-gray-900">{f.title}</h3>
                <p className="mt-2 text-sm/6 text-gray-600">{f.text}</p>
              </div>
            ))}
          </div>
        </Container>
      </section>

      {/* how it works */}
      <section id="how" className="scroll-mt-8 border-y border-gray-200 bg-surface py-24 sm:py-28">
        <Container>
          <div className="mx-auto max-w-2xl text-center reveal">
            <h2 className="text-base font-semibold text-link">How it works</h2>
            <p className="mt-2 text-3xl font-semibold tracking-tight text-balance text-gray-900 sm:text-4xl">Four steps, one job</p>
          </div>
          <div className="relative mx-auto mt-16 max-w-5xl">
          <div aria-hidden="true" className="absolute top-6 right-[12.5%] left-[12.5%] hidden h-px bg-gradient-to-r from-blue-500/0 via-blue-500/50 to-blue-500/0 lg:block" />
          <ol className="relative grid grid-cols-1 gap-10 sm:grid-cols-2 lg:grid-cols-4 lg:gap-6">
            {steps.map((s, i) => (
              <li key={s.title} className="relative reveal text-center">
                <div className="relative mx-auto flex size-12 items-center justify-center rounded-full bg-surface text-link shadow-sm ring-1 ring-gray-200">
                  <s.icon className="size-6" aria-hidden="true" />
                  <span className="absolute -top-1 -right-1 flex size-5 items-center justify-center rounded-full bg-primary text-[0.65rem] font-semibold text-white">{i + 1}</span>
                </div>
                <h3 className="mt-5 text-base font-semibold text-gray-900">{s.title}</h3>
                <p className="mx-auto mt-2 max-w-xs text-sm/6 text-gray-600">{s.text}</p>
              </li>
            ))}
          </ol>
          </div>
        </Container>
      </section>

      {/* call to action */}
      <section className="py-24 sm:py-28">
        <Container>
          <div className="relative isolate reveal overflow-hidden rounded-3xl bg-slate-950 px-6 py-16 text-center shadow-xl ring-1 ring-white/10 sm:px-16">
            <div aria-hidden="true" className="absolute -top-24 left-1/2 -z-10 size-[28rem] -translate-x-1/2 rounded-full bg-blue-600/30 blur-3xl motion-safe:animate-drift" />
            <BuildingOffice2Icon className="mx-auto size-10 text-blue-300" aria-hidden="true" />
            <h2 className="mt-4 text-3xl font-semibold tracking-tight text-balance text-white sm:text-4xl">Your workshop, organised.</h2>
            <p className="mx-auto mt-4 max-w-xl text-pretty text-slate-300">Sign in with the account your administrator created for you.</p>
            <Link href="/auth/login"
              className="group mt-8 inline-flex items-center gap-x-2 rounded-full bg-white px-6 py-3 text-sm font-semibold text-slate-900 transition hover:bg-blue-50">
              Sign in
              <ArrowRightIcon className="size-4 transition-transform group-hover:translate-x-0.5" aria-hidden="true" />
            </Link>
          </div>
        </Container>
      </section>

      <footer className="border-t border-gray-200 py-8">
        <Container className="flex flex-wrap items-center justify-between gap-4 text-sm text-gray-500">
          <span className="flex items-center gap-x-2">
            <Image alt="" src="/logo.png" width={24} height={24} className="size-5" />
            VG Auto
          </span>
          <p>&copy; {new Date().getFullYear()} V. G. Global Solution Canada Inc. All rights reserved.</p>
        </Container>
      </footer>
    </div>
  )
}
