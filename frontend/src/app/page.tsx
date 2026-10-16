// VG Auto landing page: an Apple-style scroll story of a wheel and its brake, in English or Chinese
import Link from 'next/link'
import Image from 'next/image'
import { type Metadata } from 'next'
import {
  ArchiveBoxIcon,
  ArrowRightIcon,
  EnvelopeIcon,
  LanguageIcon,
  PencilSquareIcon,
  ReceiptPercentIcon,
  TruckIcon,
  WrenchScrewdriverIcon,
} from '@heroicons/react/24/outline'
import { Container } from '@/_components/layout/Container'
import { siteDictionary } from './_landing/i18n'
import WheelStory from './_landing/WheelStory'

export async function generateMetadata(): Promise<Metadata> {
  const t = await siteDictionary()
  return { title: { absolute: t.htmlTitle } }
}

const featureIcons = [WrenchScrewdriverIcon, PencilSquareIcon, EnvelopeIcon, TruckIcon, ArchiveBoxIcon, ReceiptPercentIcon]

export default async function Home() {
  const t = await siteDictionary()

  return (
    <div lang={t.lang === 'zh' ? 'zh-CN' : 'en'} className="motion-steady min-h-full bg-gray-50">
      {/* bar: translucent black, like a product page */}
      <header className="fixed inset-x-0 top-0 z-50 border-b border-white/10 bg-black/90 backdrop-blur-xl backdrop-saturate-150">
        <Container className="flex h-14 items-center justify-between">
          <Link href="/" aria-label="VG Auto" className="flex items-center gap-x-2.5">
            <Image alt="" src="/logo.png" width={32} height={32} className="size-7" priority />
            <span className="text-sm font-semibold tracking-tight text-white">VG Auto</span>
          </Link>
          <div className="flex items-center gap-x-2">
            <a href={`/lang?to=${t.switchToLang}`} hrefLang={t.switchToLang}
              className="flex items-center gap-x-1.5 rounded-full px-3 py-1.5 text-xs font-medium text-slate-300 transition hover:bg-white/10 hover:text-white">
              <LanguageIcon className="size-4" aria-hidden="true" />
              {t.switchTo}
            </a>
            <Link href="/auth/login" className="rounded-full bg-primary px-4 py-1.5 text-xs font-semibold text-white transition hover:bg-primary-hover">
              {t.signIn}
            </Link>
          </div>
        </Container>
      </header>

      {/* hero */}
      <section className="relative isolate overflow-hidden bg-black pt-14">
        <div aria-hidden="true" className="pointer-events-none absolute inset-0 -z-10">
          <div className="absolute top-1/3 left-1/2 size-[44rem] -translate-x-1/2 rounded-full bg-blue-600/20 blur-[120px] motion-safe:animate-drift" />
          <div className="absolute inset-0 bg-[linear-gradient(to_right,rgb(255_255_255/0.035)_1px,transparent_1px),linear-gradient(to_bottom,rgb(255_255_255/0.035)_1px,transparent_1px)] bg-[size:64px_64px] [mask-image:radial-gradient(ellipse_at_center,black_25%,transparent_70%)]" />
        </div>
        <Container className="flex min-h-[calc(100svh-3.5rem)] flex-col items-center justify-center py-24 text-center">
          <p className="text-sm font-medium text-blue-300 motion-safe:animate-fade-up">{t.heroEyebrow}</p>
          <h1 className="mt-5 max-w-4xl bg-gradient-to-b from-white via-white to-slate-400 bg-clip-text text-5xl font-semibold tracking-tight text-balance text-transparent sm:text-7xl lg:text-8xl motion-safe:animate-fade-up motion-safe:[animation-delay:100ms]">
            {t.heroTitle}
          </h1>
          <p className="mt-6 max-w-2xl text-lg text-pretty text-slate-400 sm:text-xl motion-safe:animate-fade-up motion-safe:[animation-delay:200ms]">{t.heroSub}</p>
          <div className="mt-10 flex flex-wrap justify-center gap-4 motion-safe:animate-fade-up motion-safe:[animation-delay:300ms]">
            <Link href="/auth/login"
              className="group inline-flex items-center gap-x-2 rounded-full bg-primary px-7 py-3 text-sm font-semibold text-white shadow-lg shadow-blue-600/30 transition hover:bg-primary-hover">
              {t.heroCta}
              <ArrowRightIcon className="size-4 transition-transform group-hover:translate-x-0.5" aria-hidden="true" />
            </Link>
          </div>
          <a href="#story" className="mt-20 flex flex-col items-center gap-2 text-xs text-slate-500 transition hover:text-slate-300 motion-safe:animate-fade-in motion-safe:[animation-delay:700ms]">
            {t.heroMore}
            <span className="flex h-8 w-5 justify-center rounded-full ring-1 ring-slate-600">
              <span className="mt-1.5 size-1 rounded-full bg-slate-400 motion-safe:animate-scroll-dot" />
            </span>
          </a>
        </Container>
      </section>

      {/* the scroll story */}
      <div id="story">
        <WheelStory t={t} />
      </div>

      {/* features: a grid of tiles */}
      <section className="py-24 sm:py-32">
        <Container>
          <div className="mx-auto max-w-2xl text-center reveal">
            <p className="text-sm font-semibold text-link">{t.featuresKicker}</p>
            <h2 className="mt-2 text-3xl font-semibold tracking-tight text-balance text-gray-900 sm:text-5xl">{t.featuresTitle}</h2>
          </div>
          <div className="mx-auto mt-16 grid max-w-6xl grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
            {t.features.map((f, i) => {
              const Icon = featureIcons[i]
              return (
                <div key={f.title}
                  className={`group reveal relative overflow-hidden rounded-3xl bg-surface p-8 shadow-sm ring-1 ring-gray-900/5 transition duration-500 hover:-translate-y-1 hover:shadow-xl ${i === 0 || i === 3 || i === 4 ? 'lg:col-span-2' : ''}`}>
                  <div aria-hidden="true" className="absolute -top-16 -right-16 size-48 rounded-full bg-blue-500/10 blur-2xl transition duration-500 group-hover:bg-blue-500/20" />
                  <Icon className="size-8 text-link" aria-hidden="true" />
                  <h3 className="mt-6 text-xl font-semibold tracking-tight text-gray-900">{f.title}</h3>
                  <p className="mt-2 max-w-md text-sm/6 text-gray-600">{f.text}</p>
                </div>
              )
            })}
          </div>
        </Container>
      </section>

      {/* figures */}
      <section className="border-y border-gray-200 bg-surface py-20">
        <Container>
          <dl className="mx-auto grid max-w-5xl grid-cols-1 gap-10 text-center sm:grid-cols-3">
            {t.stats.map(s => (
              <div key={s.label} className="reveal">
                <dt className="sr-only">{s.label}</dt>
                <dd className="bg-gradient-to-b from-gray-900 to-gray-500 bg-clip-text text-6xl font-semibold tracking-tight text-transparent">{s.value}</dd>
                <dd className="mt-2 text-sm text-gray-600">{s.label}</dd>
              </div>
            ))}
          </dl>
        </Container>
      </section>

      {/* call to action */}
      <section className="py-24 sm:py-32">
        <Container>
          <div className="relative isolate reveal overflow-hidden rounded-[2rem] bg-black px-6 py-20 text-center shadow-xl ring-1 ring-white/10 sm:px-16">
            <div aria-hidden="true" className="absolute -top-32 left-1/2 -z-10 size-[30rem] -translate-x-1/2 rounded-full bg-blue-600/30 blur-[100px] motion-safe:animate-drift" />
            <h2 className="text-3xl font-semibold tracking-tight text-balance text-white sm:text-5xl">{t.ctaTitle}</h2>
            <p className="mx-auto mt-4 max-w-xl text-pretty text-slate-400">{t.ctaText}</p>
            <Link href="/auth/login"
              className="group mt-10 inline-flex items-center gap-x-2 rounded-full bg-white px-7 py-3 text-sm font-semibold text-slate-900 transition hover:bg-blue-50">
              {t.signIn}
              <ArrowRightIcon className="size-4 transition-transform group-hover:translate-x-0.5" aria-hidden="true" />
            </Link>
          </div>
        </Container>
      </section>

      <footer className="border-t border-gray-200 py-8">
        <Container className="flex flex-wrap items-center justify-between gap-4 text-sm text-gray-500">
          <span className="flex items-center gap-x-2">
            <Image alt="" src="/logo.png" width={24} height={24} className="size-5" />
            VG Auto
          </span>
          <p>&copy; {new Date().getFullYear()} {t.footer}</p>
        </Container>
      </footer>
    </div>
  )
}
