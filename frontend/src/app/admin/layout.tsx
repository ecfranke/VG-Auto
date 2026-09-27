import Link from 'next/link'
import { headers } from 'next/headers'
import { type Metadata } from 'next'
import { ShieldCheckIcon, ArrowLeftIcon } from '@heroicons/react/24/outline'
import { currentAccount } from '@/_lib/server/account'
import { getDictionary } from './_i18n'
import { setLanguage } from './actions'
import AdminNav from './_components/AdminNav'

export const metadata: Metadata = { title: 'Admin' }

export default async function AdminLayout({ children }: { children: React.ReactNode }) {
  const { lang, t } = await getDictionary()
  const me = await currentAccount()
  const currentPath = (await headers()).get('currentPath') ?? '/admin/users'

  return (
    <div className="min-h-full bg-gray-50">
      <header className="bg-slate-900">
        <div className="mx-auto flex max-w-6xl flex-wrap items-center gap-x-6 gap-y-3 px-4 py-3 sm:px-6">
          <Link href="/admin/users" className="flex items-center gap-x-2 text-white">
            <ShieldCheckIcon className="size-6 text-indigo-400" aria-hidden="true" />
            <span className="font-semibold">VG Auto</span>
            <span className="text-slate-400">·</span>
            <span className="text-slate-200">{t.title}</span>
          </Link>
          {me.isAdmin && <AdminNav items={[{ href: '/admin/users', label: t.users }, { href: '/admin/company', label: t.company }, { href: '/admin/audit', label: t.audit }]} />}
          <div className="ml-auto flex items-center gap-x-4 text-sm">
            <span className="hidden text-slate-300 sm:inline">
              {me.fullName} · {t.roles[me.role]}
            </span>
            <form action={setLanguage}>
              <input type="hidden" name="lang" value={lang === 'zh' ? 'en' : 'zh'} />
              <input type="hidden" name="returnTo" value={currentPath} />
              <button type="submit" className="rounded-md px-2 py-1 text-slate-200 ring-1 ring-slate-600 hover:bg-slate-800">
                {t.language}
              </button>
            </form>
            <Link href="/home/work" className="flex items-center gap-x-1 text-slate-300 hover:text-white">
              <ArrowLeftIcon className="size-4" aria-hidden="true" />
              {t.backToApp}
            </Link>
          </div>
        </div>
      </header>
      <main className="mx-auto max-w-6xl px-4 py-8 sm:px-6">
        {me.isAdmin ? children : (
          <div className="rounded-lg bg-white p-8 text-center shadow-sm ring-1 ring-gray-900/5">
            <h1 className="text-lg font-semibold text-gray-900">{t.noAccessTitle}</h1>
            <p className="mt-2 text-sm text-gray-600">{t.noAccessText}</p>
          </div>
        )}
      </main>
    </div>
  )
}
