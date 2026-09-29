'use client'

import { usePathname, useRouter } from 'next/navigation'

/** Narrows a list to one company (super administrators). */
export default function CompanyFilter({ companies, value, allLabel, label }: {
  companies: { id: string, name: string }[], value: string, allLabel: string, label: string
}) {
  const router = useRouter()
  const path = usePathname()
  return (
    <label className="flex items-center gap-x-2 text-sm text-gray-700">
      <span>{label}</span>
      <select value={value} onChange={e => router.push(e.target.value ? `${path}?company=${e.target.value}` : path)}
        className="rounded-md bg-surface py-1.5 pr-8 pl-3 text-sm text-gray-900 outline-1 -outline-offset-1 outline-gray-300 focus:outline-2 focus:-outline-offset-2 focus:outline-primary">
        <option value="">{allLabel}</option>
        {companies.map(c => <option key={c.id} value={c.id}>{c.name}</option>)}
      </select>
    </label>
  )
}
