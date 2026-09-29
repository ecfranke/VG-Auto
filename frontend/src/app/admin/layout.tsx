import Link from 'next/link'
import Image from 'next/image'
import { headers } from 'next/headers'
import { type Metadata } from 'next'
import { ArrowLeftIcon, LanguageIcon } from '@heroicons/react/24/outline'
import { currentAccount, IAccount } from '@/_lib/server/account'
import { currentTheme } from '@/_lib/server/theme'
import ThemeSwitch from '@/_components/ThemeSwitch'
import { Dictionary, getDictionary } from './_i18n'
import { setLanguage } from './actions'
import AdminNav, { INavItem } from './_components/AdminNav'

export const metadata: Metadata = { title: 'Admin' }

/** Super administrators manage every company and the built-in email; administrators manage their own company. */
function navigation(me: IAccount, t: Dictionary): INavItem[] {
  if (me.isSuperAdmin) return [
    { href: '/admin', label: t.overview, icon: 'overview', exact: true },
    { href: '/admin/companies', label: t.companies, icon: 'companies' },
    { href: '/admin/users', label: t.users, icon: 'users' },
    { href: '/admin/email', label: t.builtInEmail, icon: 'email' },
    { href: '/admin/audit', label: t.audit, icon: 'audit' },
  ]
  return [
    { href: `/admin/companies/${me.companyId}`, label: t.myCompany, icon: 'company' },
    { href: '/admin/users', label: t.users, icon: 'users' },
    { href: '/admin/audit', label: t.audit, icon: 'audit' },
  ]
}

function Brand({ subtitle }: { subtitle: string }) {
  return (
    <Link href="/admin" className="flex items-center gap-x-3">
      <Image alt="" width="50" height="50" className="h-8 w-auto" src="/logo.png" />
      <span className="flex flex-col leading-tight">
        <span className="text-base font-semibold text-white">VG Auto</span>
        <span className="text-xs text-slate-400">{subtitle}</span>
      </span>
    </Link>
  )
}

export default async function AdminLayout({ children }: { children: React.ReactNode }) {
  const { lang, t } = await getDictionary()
  const me = await currentAccount()
  const theme = await currentTheme()
  const currentPath = (await headers()).get('currentPath') ?? '/admin'
  const items = me.isAdmin ? navigation(me, t) : []
  const subtitle = me.isSuperAdmin ? t.superTitle : t.title

  const languageForm = (
    <form action={setLanguage}>
      <input type="hidden" name="lang" value={lang === 'zh' ? 'en' : 'zh'} />
      <input type="hidden" name="returnTo" value={currentPath} />
      <button type="submit" className="flex items-center gap-x-1.5 rounded-lg px-2 py-1 text-sm text-slate-300 ring-1 ring-white/10 ring-inset hover:bg-white/5 hover:text-white">
        <LanguageIcon className="size-4" aria-hidden="true" />
        {t.language}
      </button>
    </form>
  )

  return (
    <div className="min-h-full bg-gray-50">
      {/* sidebar on large screens */}
      <aside className="hidden lg:fixed lg:inset-y-0 lg:z-40 lg:flex lg:w-64 lg:flex-col">
        <div className="flex grow flex-col gap-y-6 overflow-y-auto bg-slate-950 px-6 pb-5 dark:border-r dark:border-white/10">
          <div className="flex h-16 shrink-0 items-center"><Brand subtitle={subtitle} /></div>
          <AdminNav items={items} />
          <div className="mt-auto space-y-4">
            <div className="rounded-lg bg-white/5 p-3 ring-1 ring-white/10 ring-inset">
              <div className="text-xs text-slate-400">{t.signedInAs}</div>
              <div className="truncate text-sm font-semibold text-white" title={me.fullName}>{me.fullName}</div>
              <div className="truncate text-xs text-slate-400">
                {t.roles[me.role]}{!me.isSuperAdmin && me.companyName ? ` · ${me.companyName}` : ''}
              </div>
            </div>
            <div className="flex items-center justify-between gap-x-2">
              <ThemeSwitch initial={theme} labels={t.theme} />
              {languageForm}
            </div>
            <Link href="/home/work" className="-mx-2 flex items-center gap-x-3 rounded-md p-2 text-sm/6 font-semibold text-slate-400 hover:bg-white/5 hover:text-white">
              <ArrowLeftIcon className="size-6 shrink-0" aria-hidden="true" />
              {t.backToApp}
            </Link>
          </div>
        </div>
      </aside>

      {/* header on small screens */}
      <header className="bg-slate-950 px-4 pt-3 pb-2 sm:px-6 lg:hidden">
        <div className="flex items-center gap-x-3">
          <Brand subtitle={subtitle} />
          <div className="ml-auto flex items-center gap-x-2">
            <ThemeSwitch initial={theme} labels={t.theme} />
            {languageForm}
            <Link href="/home/work" title={t.backToApp} className="rounded-lg p-1.5 text-slate-300 hover:bg-white/5 hover:text-white">
              <ArrowLeftIcon className="size-5" aria-hidden="true" />
              <span className="sr-only">{t.backToApp}</span>
            </Link>
          </div>
        </div>
        {items.length > 0 && <div className="mt-3"><AdminNav items={items} row /></div>}
      </header>

      <main className="lg:pl-64">
        <div className="mx-auto max-w-6xl px-4 py-8 sm:px-6 lg:px-8">
          {me.isAdmin ? children : (
            <div className="rounded-lg bg-surface p-8 text-center shadow-sm ring-1 ring-gray-900/5">
              <h1 className="text-lg font-semibold text-gray-900">{t.noAccessTitle}</h1>
              <p className="mt-2 text-sm text-gray-600">{t.noAccessText}</p>
            </div>
          )}
        </div>
      </main>
    </div>
  )
}
