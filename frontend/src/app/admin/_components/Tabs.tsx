'use client'

import Link from 'next/link'
import clsx from 'clsx'
import { usePathname } from 'next/navigation'

/** Tabs that are pages: the one whose address is open is active. */
export default function Tabs({ tabs }: { tabs: { href: string, label: string }[] }) {
  const path = usePathname()
  return (
    <div className="-mx-4 overflow-x-auto border-b border-gray-200 px-4 sm:mx-0 sm:px-0">
      <nav className="-mb-px flex gap-x-6">
        {tabs.map(tab => {
          const active = path === tab.href
          return (
            <Link key={tab.href} href={tab.href} aria-current={active ? 'page' : undefined}
              className={clsx('border-b-2 px-1 py-3 text-sm font-medium whitespace-nowrap',
                active ? 'border-primary text-link' : 'border-transparent text-gray-500 hover:border-gray-300 hover:text-gray-700')}>
              {tab.label}
            </Link>
          )
        })}
      </nav>
    </div>
  )
}
