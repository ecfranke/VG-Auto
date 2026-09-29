import { type Metadata } from 'next'
import { CheckCircleIcon, ClockIcon, ExclamationTriangleIcon } from '@heroicons/react/20/solid'
import { errorMessage, ISigning, signingApi, TOKEN_PATTERN } from '@/_lib/server/signing'
import SignForm from './SignForm'

export const metadata: Metadata = {
  title: 'Estimate',
  robots: { index: false, follow: false },
  // the link is the key to the estimate: never send it to other sites
  referrer: 'no-referrer',
}

const dateFormat = new Intl.DateTimeFormat('en-CA', { dateStyle: 'long' })
const dateTimeFormat = new Intl.DateTimeFormat('en-CA', { dateStyle: 'long', timeStyle: 'short', timeZone: 'UTC' })

function Shell({ children, company }: { children: React.ReactNode, company?: ISigning }) {
  return (
    <div className="min-h-full bg-gray-50">
      <header className="bg-slate-950">
        <div className="mx-auto flex max-w-4xl flex-wrap items-center justify-between gap-x-6 gap-y-1 px-4 py-4 sm:px-6">
          <span className="text-lg font-semibold text-white">{company?.companyName ?? 'Estimate'}</span>
          {company && (
            <span className="text-sm text-slate-400">
              {[company.companyPhone, company.companyEmail].filter(Boolean).join(' · ')}
            </span>
          )}
        </div>
      </header>
      <main className="mx-auto max-w-4xl space-y-6 px-4 py-8 sm:px-6">{children}</main>
    </div>
  )
}

function Problem({ title, text }: { title: string, text: string }) {
  return (
    <Shell>
      <div className="rounded-lg bg-surface p-8 text-center shadow-sm ring-1 ring-gray-900/5">
        <ExclamationTriangleIcon className="mx-auto size-10 text-yellow-500" aria-hidden="true" />
        <h1 className="mt-3 text-lg font-semibold text-gray-900">{title}</h1>
        <p className="mt-2 text-sm text-gray-600">{text}</p>
      </div>
    </Shell>
  )
}

/** Where the client reviews and signs an estimate from the link in its email; no sign in needed. */
export default async function SignPage({ params }: { params: Promise<{ token: string }> }) {
  const { token } = await params
  if (!TOKEN_PATTERN.test(token)) return <Problem title="Link not valid" text="Please check the link in your email." />
  const response = await signingApi(token, 'view')
  if (!response.ok) {
    return <Problem title="Estimate not available" text={await errorMessage(response, 'The estimate could not be opened. Please try again later.')} />
  }
  const estimate = await response.json() as ISigning

  return (
    <Shell company={estimate}>
      <div className="rounded-lg bg-surface p-6 shadow-sm ring-1 ring-gray-900/5">
        <p className="text-sm text-gray-500">Estimate for {estimate.clientName ?? 'you'}</p>
        <h1 className="mt-1 font-mono text-lg font-semibold break-all text-gray-900">{estimate.code}</h1>
        <dl className="mt-4 grid grid-cols-1 gap-4 text-sm sm:grid-cols-3">
          {estimate.vehicleTitle && (
            <div>
              <dt className="text-gray-500">Vehicle</dt>
              <dd className="mt-1 font-medium text-gray-900">{estimate.vehicleTitle}</dd>
            </div>
          )}
          <div>
            <dt className="text-gray-500">Total</dt>
            <dd className="mt-1 font-medium text-gray-900">{estimate.total}</dd>
          </div>
          <div>
            <dt className="text-gray-500">Issued</dt>
            <dd className="mt-1 font-medium text-gray-900">{dateFormat.format(new Date(estimate.issuedOn))}</dd>
          </div>
        </dl>
      </div>

      {estimate.status === 'signed' && (
        <div className="flex flex-wrap items-center justify-between gap-4 rounded-lg bg-green-50 p-4 ring-1 ring-green-600/20 ring-inset">
          <div className="flex items-start gap-x-3">
            <CheckCircleIcon className="size-6 shrink-0 text-green-600" aria-hidden="true" />
            <div className="text-sm text-green-800">
              <p className="font-semibold">Signed by {estimate.signerName}</p>
              <p>{estimate.signedAt && dateTimeFormat.format(new Date(estimate.signedAt))} UTC. Thank you! {estimate.companyName} has received your signature.</p>
            </div>
          </div>
          <a href={`/sign/${token}/pdf`} className="rounded-md bg-primary px-3 py-2 text-sm font-semibold text-white shadow-xs hover:bg-primary-hover">
            Download signed estimate (PDF)
          </a>
        </div>
      )}

      {estimate.status === 'expired' && (
        <div className="flex items-start gap-x-3 rounded-lg bg-yellow-50 p-4 text-sm text-yellow-800 ring-1 ring-yellow-600/20 ring-inset">
          <ClockIcon className="size-5 shrink-0 text-yellow-600" aria-hidden="true" />
          <p>This link expired on {dateFormat.format(new Date(estimate.validUntil))}. Please ask {estimate.companyName} to send the estimate again.</p>
        </div>
      )}

      <section aria-label="Estimate" className="overflow-x-auto rounded-lg bg-white shadow-sm ring-1 ring-gray-900/5">
        {/* a document looks like paper in the dark theme too */}
        {/* laid out like the printed page; scaled down to fit a phone (pinch to zoom in) */}
        <div className="force-light min-w-[600px] bg-white p-6 text-gray-900 max-sm:[zoom:0.5] sm:p-8" dangerouslySetInnerHTML={{ __html: estimate.html }} />
      </section>

      {estimate.status === 'open' && (
        <div className="rounded-lg bg-surface p-6 shadow-sm ring-1 ring-gray-900/5">
          <h2 className="text-base font-semibold text-gray-900">Accept and sign</h2>
          <p className="mt-1 text-sm text-gray-500">
            Sign to accept this estimate. {estimate.companyName} receives the signed estimate; you can download it after signing.
            This link is valid until {dateFormat.format(new Date(estimate.validUntil))}.
          </p>
          <div className="mt-6">
            <SignForm token={token} defaultName={estimate.clientName ?? ''} />
          </div>
        </div>
      )}

      {estimate.status !== 'signed' && (
        <p className="text-center">
          <a href={`/sign/${token}/pdf`} className="text-sm text-gray-500 hover:text-gray-700">Download the estimate as PDF</a>
        </p>
      )}
    </Shell>
  )
}
