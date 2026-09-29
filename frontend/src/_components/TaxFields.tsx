'use client'

import { useState } from 'react'
import type { ITaxCountry, ITaxOptions } from '@/_lib/shared/taxes'

export interface ITaxLabels {
  country: string
  region: string
  chooseRegion: string
  tax1: string
  tax2: string
  taxName: string
  rate: string
  hint: string
  regionAdminOnly: string
}

export const englishTaxLabels: ITaxLabels = {
  country: 'Country of registration',
  region: 'Province / state',
  chooseRegion: 'Choose…',
  tax1: 'Tax 1',
  tax2: 'Tax 2 (optional)',
  taxName: 'Name',
  rate: 'Rate (%)',
  hint: 'Prices are entered before tax. Choosing the province fills in its taxes (for example GST 5% + PST 7% in British Columbia, HST 13% in Ontario); names and rates can still be changed.',
  regionAdminOnly: 'The place of registration is set by an administrator.',
}

const inputClass = 'mt-1 block w-full rounded-md bg-surface px-3 py-1.5 text-sm text-gray-900 outline-1 -outline-offset-1 outline-gray-300 placeholder:text-gray-400 focus:outline-2 focus:-outline-offset-2 focus:outline-primary disabled:bg-gray-50 disabled:text-gray-500'
const labelClass = 'block text-sm/6 font-medium text-gray-900'

const rateText = (rate: number | null | undefined) => (rate === null || rate === undefined ? '' : String(rate))

/**
 * Country and province of registration plus up to two sales taxes. Picking a province fills in the taxes
 * charged there; the names and rates stay editable. Field names: taxCountry, taxRegion, tax1Name, tax1Rate, tax2Name, tax2Rate.
 */
export default function TaxFields({ countries, value, canChangeRegion = true, labels = englishTaxLabels }: {
  countries: ITaxCountry[]
  value: ITaxOptions | null | undefined
  canChangeRegion?: boolean
  labels?: ITaxLabels
}) {
  const [country, setCountry] = useState(value?.country ?? 'CA')
  const [region, setRegion] = useState(value?.region ?? '')
  const [tax1Name, setTax1Name] = useState(value?.tax1Name ?? '')
  const [tax1Rate, setTax1Rate] = useState(rateText(value?.tax1Rate))
  const [tax2Name, setTax2Name] = useState(value?.tax2Name ?? '')
  const [tax2Rate, setTax2Rate] = useState(value?.tax2Name ? rateText(value?.tax2Rate) : '')

  const current = countries.find(c => c.code === country)

  const apply = (taxes: { name: string, rate: number }[]) => {
    setTax1Name(taxes[0]?.name ?? '')
    setTax1Rate(rateText(taxes[0]?.rate))
    setTax2Name(taxes[1]?.name ?? '')
    setTax2Rate(taxes[1] ? rateText(taxes[1].rate) : '')
  }

  const chooseCountry = (code: string) => {
    setCountry(code)
    setRegion('')
    const next = countries.find(c => c.code === code)
    if (next) apply(next.defaultTaxes)
  }

  const chooseRegion = (code: string) => {
    setRegion(code)
    const found = current?.regions.find(r => r.code === code)
    if (found) apply(found.taxes)
  }

  return (
    <div className="grid grid-cols-1 gap-4 sm:grid-cols-4">
      <div className="sm:col-span-2">
        <label htmlFor="taxCountry" className={labelClass}>{labels.country}</label>
        <select id="taxCountry" name="taxCountry" value={country} disabled={!canChangeRegion}
          onChange={e => chooseCountry(e.target.value)} className={inputClass}>
          {countries.map(c => <option key={c.code} value={c.code}>{c.name}</option>)}
        </select>
      </div>
      <div className="sm:col-span-2">
        <label htmlFor="taxRegion" className={labelClass}>{labels.region}</label>
        {current && current.regions.length > 0
          ? <select id="taxRegion" name="taxRegion" value={region} disabled={!canChangeRegion}
              onChange={e => chooseRegion(e.target.value)} className={inputClass}>
              <option value="">{labels.chooseRegion}</option>
              {current.regions.map(r => <option key={r.code} value={r.code}>{r.name} ({r.code})</option>)}
            </select>
          : <input id="taxRegion" name="taxRegion" value={region} disabled={!canChangeRegion} maxLength={10}
              onChange={e => setRegion(e.target.value.toUpperCase())} className={inputClass} />}
      </div>
      {!canChangeRegion && <>
        {/* disabled fields are not submitted; the API keeps the place of registration for non-administrators anyway */}
        <input type="hidden" name="taxCountry" value={country} />
        <input type="hidden" name="taxRegion" value={region} />
      </>}

      <div>
        <label htmlFor="tax1Name" className={labelClass}>{labels.tax1} · {labels.taxName}</label>
        <input id="tax1Name" name="tax1Name" value={tax1Name} required maxLength={30} onChange={e => setTax1Name(e.target.value)} className={inputClass} />
      </div>
      <div>
        <label htmlFor="tax1Rate" className={labelClass}>{labels.rate}</label>
        <input id="tax1Rate" name="tax1Rate" type="number" min={0} max={100} step="0.001" value={tax1Rate} required
          onChange={e => setTax1Rate(e.target.value)} className={inputClass} />
      </div>
      <div>
        <label htmlFor="tax2Name" className={labelClass}>{labels.tax2} · {labels.taxName}</label>
        <input id="tax2Name" name="tax2Name" value={tax2Name} maxLength={30} onChange={e => setTax2Name(e.target.value)} className={inputClass} />
      </div>
      <div>
        <label htmlFor="tax2Rate" className={labelClass}>{labels.rate}</label>
        <input id="tax2Rate" name="tax2Rate" type="number" min={0} max={100} step="0.001" value={tax2Rate}
          onChange={e => setTax2Rate(e.target.value)} className={inputClass} />
      </div>
      <p className="text-xs text-gray-500 sm:col-span-4">{labels.hint}{!canChangeRegion && ' ' + labels.regionAdminOnly}</p>
    </div>
  )
}
