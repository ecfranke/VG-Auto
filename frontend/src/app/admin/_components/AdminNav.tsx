'use client'

import Link from 'next/link'
import clsx from 'clsx'
import { usePathname } from 'next/navigation'
import {
  BuildingOffice2Icon,
  ClipboardDocumentListIcon,
  EnvelopeIcon,
  HomeIcon,
  Squares2X2Icon,
  UsersIcon,
  WrenchScrewdriverIcon,
} from '@heroicons/react/24/outline'

const icons = {
  overview: Squares2X2Icon,
  companies: BuildingOffice2Icon,
  company: HomeIcon,
  users: UsersIcon,
  works: WrenchScrewdriverIcon,
  email: EnvelopeIcon,
  audit: ClipboardDocumentListIcon,
}

export interface INavItem {
  href: string
  label: string
  icon: keyof typeof icons
  /** active only on this very page, not below it */
  exact?: boolean
}

/** Menu of the administration: a column in the sidebar, a scrolling row on small screens. */
export default function AdminNav({ items, row }: { items: INavItem[], row?: boolean }) {
  const path = usePathname()
  return (
    <nav className={row ? '-mx-4 overflow-x-auto px-4' : undefined}>
      <ul role="list" className={row ? 'flex gap-x-1' : '-mx-2 space-y-1'}>
        {items.map(item => {
          const active = item.exact ? path === item.href : path === item.href || path.startsWith(item.href + '/')
          const Icon = icons[item.icon]
          return (
            <li key={item.href}>
              <Link href={item.href} aria-current={active ? 'page' : undefined}
                className={clsx('group flex items-center gap-x-3 rounded-md font-semibold whitespace-nowrap',
                  row ? 'px-3 py-1.5 text-sm' : 'p-2 text-sm/6',
                  active ? 'bg-white/10 text-white' : 'text-slate-400 hover:bg-white/5 hover:text-white')}>
                <Icon aria-hidden="true" className={clsx('shrink-0', row ? 'size-5' : 'size-6', active && 'text-blue-400')} />
                {item.label}
              </Link>
            </li>
          )
        })}
      </ul>
    </nav>
  )
}
