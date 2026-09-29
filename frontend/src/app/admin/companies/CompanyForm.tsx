'use client'

import { saveCompany, createCompany, ActionState } from '../actions'
import type { Dictionary } from '../_i18n'
import { Alert, Field, SubmitButton } from '../_components/Fields'
import { useFormAction } from '../_components/useFormAction'
import TaxFields from '@/_components/TaxFields'
import type { ITaxCountry, ITaxOptions } from '@/_lib/shared/taxes'

export interface ICompanyOptions {
  requisites: { name: string, phone: string, address: string, email: string, bankAccount: string, regNr: string, kmkr: string }
  pricing: {
    invoice: { vatRate: number, surCharge: string, disclaimer: string, signatureLine: boolean, emailContent: string, showBankAccount?: boolean, showRegNo?: boolean }
    estimate: { emailContent: string }
    currency: string
    taxes: ITaxOptions | null
  }
}

const areaClass = 'mt-1 block w-full rounded-md bg-surface px-3 py-1.5 text-sm text-gray-900 outline-1 -outline-offset-1 outline-gray-300 focus:outline-2 focus:-outline-offset-2 focus:outline-primary'

function Area({ label, name, defaultValue, rows = 4 }: { label: string, name: string, defaultValue?: string | null, rows?: number }) {
  return (
    <div className="sm:col-span-2">
      <label htmlFor={name} className="block text-sm/6 font-medium text-gray-900">{label}</label>
      <textarea id={name} name={name} rows={rows} defaultValue={defaultValue ?? ''} className={areaClass} />
    </div>
  )
}

function Section({ title, hint, children }: { title: string, hint?: string, children: React.ReactNode }) {
  return (
    <section className="rounded-lg bg-surface p-6 shadow-sm ring-1 ring-gray-900/5">
      <h2 className="text-base font-semibold text-gray-900">{title}</h2>
      {hint && <p className="mt-1 text-sm text-gray-500">{hint}</p>}
      <div className="mt-4">{children}</div>
    </section>
  )
}

export function CompanyForm({ t, companyId, options, currencies, countries }: { t: Dictionary, companyId: string, options: ICompanyOptions, currencies: { code: string, name: string }[], countries: ITaxCountry[] }) {
  const [state, onSubmit, pending] = useFormAction<ActionState>(saveCompany, {})
  const r = options.requisites
  const inv = options.pricing.invoice
  return (
    <form onSubmit={onSubmit} className="space-y-6">
      <input type="hidden" name="companyId" value={companyId} />
      {state.error && <Alert kind="error">{state.error}</Alert>}
      {state.ok && <Alert kind="success">{t.saved}</Alert>}

      <Section title={t.companyInfo} hint={t.companyHint}>
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
          <Field label={t.companyName} name="name" defaultValue={r.name} required />
          <Field label={t.regNo} name="regNr" defaultValue={r.regNr} />
          <Field label={t.taxId} name="kmkr" defaultValue={r.kmkr} />
          <div>
            <label htmlFor="currency" className="block text-sm/6 font-medium text-gray-900">{t.currency}</label>
            <select id="currency" name="currency" defaultValue={options.pricing.currency} className={areaClass}>
              {currencies.map(c => <option key={c.code} value={c.code}>{c.code} · {c.name}</option>)}
            </select>
            <p className="mt-1 text-xs text-gray-500">{t.currencyHint}</p>
          </div>
          <Field label={t.phone} name="phone" defaultValue={r.phone} />
          <Field label={t.email} name="email" type="email" defaultValue={r.email} />
          <Field label={t.address} name="address" defaultValue={r.address} className="sm:col-span-2" />
          <Field label={t.bankAccount} name="bankAccount" defaultValue={r.bankAccount} className="sm:col-span-2" />
        </div>
      </Section>

      <Section title={t.taxes}>
        <TaxFields countries={countries} value={options.pricing.taxes} labels={t.taxLabels} />
      </Section>

      <Section title={t.invoiceOptions}>
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
          <Field label={t.surcharge} name="surCharge" defaultValue={inv.surCharge} />
          <label className="flex items-center gap-x-2 text-sm font-medium text-gray-900 sm:col-span-2">
            <input type="checkbox" name="signatureLine" defaultChecked={inv.signatureLine} className="size-4 rounded border-gray-300 accent-primary" />
            {t.signatureLine}
          </label>
          <label className="flex items-center gap-x-2 text-sm font-medium text-gray-900 sm:col-span-2">
            <input type="checkbox" name="showBankAccount" defaultChecked={!!inv.showBankAccount} className="size-4 rounded border-gray-300 accent-primary" />
            {t.showBankAccount}
          </label>
          <label className="flex items-center gap-x-2 text-sm font-medium text-gray-900 sm:col-span-2">
            <input type="checkbox" name="showRegNo" defaultChecked={!!inv.showRegNo} className="size-4 rounded border-gray-300 accent-primary" />
            {t.showRegNo}
          </label>
          <Area label={t.disclaimer} name="disclaimer" defaultValue={inv.disclaimer} />
          <Area label={t.invoiceEmail} name="emailContent" defaultValue={inv.emailContent} rows={6} />
        </div>
      </Section>

      <Section title={t.offerOptions}>
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
          <Area label={t.offerEmail} name="estimateEmailContent" defaultValue={options.pricing.estimate.emailContent} rows={6} />
        </div>
      </Section>

      <div className="flex justify-end"><SubmitButton pending={pending}>{t.save}</SubmitButton></div>
    </form>
  )
}

export function NewCompanyForm({ t, currencies }: { t: Dictionary, currencies: { code: string, name: string }[] }) {
  const [state, onSubmit, pending] = useFormAction<ActionState>(createCompany, {})
  return (
    <Section title={t.newCompany}>
      <form onSubmit={onSubmit} className="space-y-3">
        {state.error && <Alert kind="error">{state.error}</Alert>}
        <div className="flex flex-wrap items-end gap-3">
          <Field label={t.companyName} name="name" required className="w-80" />
          <div>
            <label htmlFor="newCurrency" className="block text-sm/6 font-medium text-gray-900">{t.currency}</label>
            <select id="newCurrency" name="currency" defaultValue="CAD" className={areaClass}>
              {currencies.map(c => <option key={c.code} value={c.code}>{c.code} · {c.name}</option>)}
            </select>
          </div>
          <SubmitButton pending={pending}>{t.createCompany}</SubmitButton>
        </div>
        <label className="flex items-start gap-x-2 text-sm text-gray-900">
          <input type="checkbox" name="allowSystemEmail" defaultChecked className="mt-0.5 size-4 rounded border-gray-300 accent-primary" />
          <span><span className="font-medium">{t.allowBuiltIn}</span><span className="block text-gray-500">{t.allowBuiltInHint}</span></span>
        </label>
      </form>
    </Section>
  )
}
