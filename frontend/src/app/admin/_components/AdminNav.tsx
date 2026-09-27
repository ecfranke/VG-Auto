'use client'

import Link from 'next/link'
import clsx from 'clsx'
import { usePathname } from 'next/navigation'

export default function AdminNav({ items }: { items: { href: string, label: string }[] }) {
  const path = usePathname()
  return (
    <nav className="flex gap-x-1">
      {items.map(item => (
        <Link key={item.href} href={item.href}
          className={clsx('rounded-md px-3 py-1.5 text-sm font-medium',
            path.startsWith(item.href) ? 'bg-slate-800 text-white' : 'text-slate-300 hover:bg-slate-800 hover:text-white')}>
          {item.label}
        </Link>
      ))}
    </nav>
  )
}
