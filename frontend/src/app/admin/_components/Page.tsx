import Link from 'next/link'
import clsx from 'clsx'

export function PageHeader({ title, hint, back, children }: {
  title: React.ReactNode, hint?: React.ReactNode, back?: { href: string, label: string }, children?: React.ReactNode
}) {
  return (
    <div>
      {back && <Link href={back.href} className="text-sm text-gray-500 hover:text-gray-700">← {back.label}</Link>}
      <div className={clsx('flex flex-wrap items-center justify-between gap-3', back && 'mt-2')}>
        <h1 className="text-xl font-semibold text-gray-900">{title}</h1>
        {children && <div className="flex flex-wrap items-center gap-3">{children}</div>}
      </div>
      {hint && <p className="mt-1 max-w-3xl text-sm text-gray-500">{hint}</p>}
    </div>
  )
}

export function Card({ title, hint, actions, children, className }: {
  title?: React.ReactNode, hint?: React.ReactNode, actions?: React.ReactNode, children: React.ReactNode, className?: string
}) {
  return (
    <section className={clsx('rounded-lg bg-surface p-6 shadow-sm ring-1 ring-gray-900/5', className)}>
      {(title || actions) && (
        <div className="flex flex-wrap items-baseline justify-between gap-2">
          {title && <h2 className="text-base font-semibold text-gray-900">{title}</h2>}
          {actions}
        </div>
      )}
      {hint && <p className="mt-1 text-sm text-gray-500">{hint}</p>}
      <div className={clsx((title || hint || actions) && 'mt-4')}>{children}</div>
    </section>
  )
}

export function Stat({ label, value, href, tone }: { label: string, value: React.ReactNode, href?: string, tone?: 'warning' }) {
  const body = (
    <>
      <dt className="truncate text-sm font-medium text-gray-500">{label}</dt>
      <dd className={clsx('mt-1 text-3xl font-semibold tracking-tight', tone === 'warning' ? 'text-yellow-700' : 'text-gray-900')}>{value}</dd>
    </>
  )
  const className = 'block rounded-lg bg-surface px-4 py-5 shadow-sm ring-1 ring-gray-900/5 sm:p-6'
  return href
    ? <Link href={href} className={clsx(className, 'hover:ring-primary/40')}>{body}</Link>
    : <div className={className}>{body}</div>
}

/** Table in a card; the header cells are given as strings or nodes. */
export function Table({ head, children, empty, emptyText }: {
  head: (React.ReactNode | { label: React.ReactNode, right?: boolean })[], children: React.ReactNode, empty?: boolean, emptyText?: string
}) {
  return (
    <div className="overflow-x-auto rounded-lg bg-surface shadow-sm ring-1 ring-gray-900/5">
      <table className="min-w-full divide-y divide-gray-200 text-sm">
        <thead className="bg-gray-50 text-left text-gray-900">
          <tr>
            {head.map((h, i) => {
              const cell = h && typeof h === 'object' && 'label' in h ? h : { label: h as React.ReactNode, right: false }
              return <th key={i} className={clsx('px-4 py-3 font-semibold whitespace-nowrap', cell.right && 'text-right')}>{cell.label}</th>
            })}
          </tr>
        </thead>
        <tbody className="divide-y divide-gray-100">
          {children}
          {empty && <tr><td colSpan={head.length} className="px-4 py-6 text-center text-gray-500">{emptyText}</td></tr>}
        </tbody>
      </table>
    </div>
  )
}
