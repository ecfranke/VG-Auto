// VG Auto landing page
import { Button } from '@/_components/layout/Button'
import { Container } from '@/_components/layout/Container'
import { Header } from '@/_components/layout/Header'
import { WrenchScrewdriverIcon, UsersIcon, ArchiveBoxIcon, EnvelopeIcon } from '@heroicons/react/24/outline'

const SOURCE_URL = 'https://github.com/ecfranke/VG-Auto'

const features = [
  { icon: WrenchScrewdriverIcon, title: 'Work orders', text: 'Estimates, repair jobs, parts and labour in one place. Turn an accepted estimate into a job with one click.' },
  { icon: UsersIcon, title: 'Clients & vehicles', text: 'Full history of every customer and car: estimates, invoices and work done.' },
  { icon: ArchiveBoxIcon, title: 'Inventory', text: 'Spare parts with storage locations, prices and discounts, linked to your work orders.' },
  { icon: EnvelopeIcon, title: 'Invoices by email', text: 'PDF estimates and invoices sent straight to the client via SMTP or Microsoft 365.' },
]

export default function Home() {
  return (
    <>
      <Header />
      <main>
        <Container className="pt-6 pb-12 text-center lg:pt-12">
          <h1 className="mx-auto max-w-4xl font-display text-4xl font-bold tracking-tight text-slate-900 sm:text-5xl">
            VG Auto
          </h1>
          <p className="mx-auto mt-3 max-w-2xl text-xl tracking-tight text-indigo-600">
            Workshop management for your repair shop
          </p>
          <p className="mx-auto mt-4 max-w-2xl text-base tracking-tight text-slate-700">
            Manage repairs, vehicles, clients, parts and invoices in one clean interface.
          </p>
          <div className="mt-8 flex justify-center gap-x-6">
            <Button href="/auth/login" color="blue">Sign in</Button>
          </div>
        </Container>

        <Container className="mb-16">
          <div className="grid grid-cols-1 gap-y-10 sm:grid-cols-2 sm:gap-x-6 lg:grid-cols-4">
            {features.map((f) => (
              <div key={f.title} className="text-center">
                <f.icon className="mx-auto mb-3 h-12 w-12 text-indigo-600" aria-hidden="true" />
                <h3 className="text-lg font-semibold text-gray-900">{f.title}</h3>
                <p className="mt-2 text-sm text-gray-600">{f.text}</p>
              </div>
            ))}
          </div>
        </Container>
      </main>
      <footer className="border-t border-slate-200 py-8">
        <Container className="flex flex-col items-center gap-2 text-sm text-slate-500 sm:flex-row sm:justify-between">
          <p>&copy; {new Date().getFullYear()} VG Auto</p>
          <p>
            Free software under the{' '}
            <a className="underline hover:text-slate-700" href="https://www.gnu.org/licenses/agpl-3.0.html">GNU AGPL v3</a>
            {' · '}
            <a className="underline hover:text-slate-700" href={SOURCE_URL}>Source code</a>
          </p>
        </Container>
      </footer>
    </>
  )
}
