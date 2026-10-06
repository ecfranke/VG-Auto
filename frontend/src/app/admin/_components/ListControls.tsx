import Link from 'next/link'

const inputClass = 'rounded-md bg-surface px-3 py-1.5 text-sm text-gray-900 outline-1 -outline-offset-1 outline-gray-300 placeholder:text-gray-400 focus:outline-2 focus:-outline-offset-2 focus:outline-primary'

/** Search box, company filter (super administrators) and paging of the admin lists. */
export function ListSearch({ q, company, companies, placeholder, searchLabel, companyLabel, allCompaniesLabel }: {
  q: string, company: string, companies: { id: string, name: string }[] | null, placeholder: string, searchLabel: string,
  companyLabel: string, allCompaniesLabel: string
}) {
  return (
    <form className="flex flex-wrap items-end gap-3">
      <input name="q" defaultValue={q} placeholder={placeholder} aria-label={searchLabel} className={`${inputClass} w-full sm:w-96`} />
      {companies && (
        <select name="company" defaultValue={company} aria-label={companyLabel} className={inputClass}>
          <option value="">{allCompaniesLabel}</option>
          {companies.map(c => <option key={c.id} value={c.id}>{c.name}</option>)}
        </select>
      )}
      <button type="submit" className="rounded-md bg-primary px-3 py-2 text-sm font-semibold text-white shadow-xs hover:bg-primary-hover">{searchLabel}</button>
    </form>
  )
}

export function Pager({ page, hasMore, href, previous, next }: { page: number, hasMore: boolean, href: (page: number) => string, previous: string, next: string }) {
  return (
    <div className="flex justify-end gap-x-2 text-sm">
      {page > 0 && <Link href={href(page - 1)} className="rounded-md bg-surface px-3 py-1.5 text-gray-700 ring-1 ring-gray-300 hover:bg-gray-50">{previous}</Link>}
      {hasMore && <Link href={href(page + 1)} className="rounded-md bg-surface px-3 py-1.5 text-gray-700 ring-1 ring-gray-300 hover:bg-gray-50">{next}</Link>}
    </div>
  )
}

/** Reads q, company and page of a list; builds the links that keep them. */
export function listParams(path: string, query: { q?: string, company?: string, page?: string }, superAdmin: boolean) {
  const q = (query.q ?? '').trim()
  const company = superAdmin && /^[0-9a-fA-F-]{36}$/.test(query.company ?? '') ? query.company! : ''
  const page = Math.max(0, parseInt(query.page ?? '0', 10) || 0)
  const href = (p: number) => {
    const next = new URLSearchParams()
    if (q) next.set('q', q)
    if (company) next.set('company', company)
    if (p > 0) next.set('page', String(p))
    const s = next.toString()
    return s ? `${path}?${s}` : path
  }
  const api = (limit: number) => {
    const params = new URLSearchParams({ limit: String(limit), offset: String(page * limit) })
    if (q) params.set('searchText', q)
    if (company) params.set('companyId', company)
    return params.toString()
  }
  return { q, company, page, href, api }
}
