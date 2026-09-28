/** Sales taxes of a company (API: pricing.taxes). */
export interface ITaxOptions {
  country: string | null
  region: string | null
  tax1Name: string
  tax1Rate: number
  tax2Name: string | null
  tax2Rate: number
}

export interface ITaxRate { name: string, rate: number }

export interface ITaxRegion { country: string, code: string, name: string, taxIdLabel: string, taxes: ITaxRate[] }

/** API: GET options/taxregions */
export interface ITaxCountry { code: string, name: string, regions: ITaxRegion[], taxIdLabel: string, defaultTaxes: ITaxRate[] }

/** A tax with its amount (price summaries). */
export interface ITaxAmount { name: string, rate: number, amount: number }

/** Reads the fields of the TaxFields component; returns an error message or the taxes. */
export function taxesFromForm(form: FormData): { error: string } | { taxes: ITaxOptions } {
  const text = (name: string) => (form.get(name)?.toString() ?? '').trim()
  const rate = (name: string) => {
    const value = text(name)
    if (value === '') return 0
    const n = Number(value.replace(',', '.'))
    return Number.isFinite(n) && n >= 0 && n <= 100 ? n : NaN
  }
  const tax1Name = text('tax1Name')
  const tax2Name = text('tax2Name')
  const tax1Rate = rate('tax1Rate')
  const tax2Rate = rate('tax2Rate')
  if (!tax1Name) return { error: 'Enter the name of the tax (for example GST or HST).' }
  if (Number.isNaN(tax1Rate) || Number.isNaN(tax2Rate)) return { error: 'Tax rates must be numbers between 0 and 100.' }
  return {
    taxes: {
      country: text('taxCountry') || null,
      region: text('taxRegion') || null,
      tax1Name,
      tax1Rate,
      tax2Name: tax2Name || null,
      tax2Rate: tax2Name ? tax2Rate : 0,
    },
  }
}

/** "GST 5% + PST 7%" */
export function describeTaxes(t: ITaxOptions | null | undefined): string {
  if (!t) return ''
  const one = (name: string | null, rate: number) => (name ? `${name} ${rate}%` : '')
  return [one(t.tax1Name, t.tax1Rate), one(t.tax2Name, t.tax2Rate)].filter(Boolean).join(' + ')
}
