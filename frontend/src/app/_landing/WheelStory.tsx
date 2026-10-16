'use client'

import { useEffect, useRef } from 'react'

export interface WheelStoryText {
  story: { kicker: string, title: string, text: string }[]
  parts: { tyre: string, rim: string, disc: string, caliper: string, hub: string }
  estimateTitle: string
  estimateLines: { name: string, price: string }[]
  estimateTax: string
  estimateTaxAmount: string
  estimateTotal: string
  estimateTotalAmount: string
  signed: string
  invoiced: string
}

// ------------------------------------------------------------------ timeline (p = 0 … 1 over the pinned section)

const clamp = (v: number) => Math.min(1, Math.max(0, v))
const smooth = (v: number) => v * v * (3 - 2 * v)
/** 0 before a, 1 after b, eased in between */
const seg = (p: number, a: number, b: number) => smooth(clamp((p - a) / (b - a)))

/** How far the wheel is taken apart: apart from 0.12 to 0.42, together again from 0.80 to 0.95. */
const explodeAt = (p: number) => seg(p, 0.12, 0.42) * (1 - seg(p, 0.8, 0.95))

// the parts, front (towards the viewer, left) to back; x is the offset when taken apart
const PARTS = {
  tyre: { x: -300, scale: 1 },
  rim: { x: -120, scale: 0.97 },
  disc: { x: 70, scale: 0.92 },
  hub: { x: 240, scale: 0.86 },
} as const
type PartName = keyof typeof PARTS

const CX = 520
const CY = 300
/** the wheel is seen at an angle: circles become ellipses */
const SQUASH = 0.56

function partTransform(part: PartName, p: number) {
  const e = explodeAt(p)
  const { x, scale } = PARTS[part]
  // big and close while whole, smaller when apart so every part fits
  const zoom = (0.95 + 0.3 * seg(p, 0, 0.12)) * (1 - 0.25 * e)
  const s = (1 - e + e * scale) * zoom
  const f = (v: number) => Number(v.toFixed(4))
  return `translate(${f(CX + x * e * zoom)} ${CY}) scale(${f(SQUASH * s)} ${f(s)})`
}

/** the rotating parts turn with the scroll, like the wheel rolling */
const spin = (p: number) => `rotate(${Number((p * 540).toFixed(2))})`

const labelOpacity = (p: number) => seg(p, 0.34, 0.44) * (1 - seg(p, 0.6, 0.68))
const heatOpacity = (p: number) => 0.85 * seg(p, 0.44, 0.58) * (1 - seg(p, 0.72, 0.82))
const captionOpacity = (p: number, i: number) => {
  const ranges = [[-1, 0, 0.22, 0.28], [0.24, 0.3, 0.5, 0.56], [0.52, 0.58, 0.78, 0.84], [0.8, 0.86, 2, 2]]
  const [a, b, c, d] = ranges[i]
  return seg(p, a, b) * (1 - seg(p, c, d))
}
const estimateOpacity = (p: number) => seg(p, 0.56, 0.62) * (1 - seg(p, 0.9, 0.97))
const lineOpacity = (p: number, i: number) => seg(p, 0.6 + i * 0.035, 0.63 + i * 0.035)
const signedOpacity = (p: number) => seg(p, 0.76, 0.8)
const invoiceOpacity = (p: number) => seg(p, 0.9, 0.96)

// ------------------------------------------------------------------ drawing (circle space, centre 0,0)

function Defs() {
  return (
    <defs>
      <radialGradient id="ws-tyre" cx="50%" cy="50%" r="50%">
        <stop offset="70%" stopColor="#0b0c0e" />
        <stop offset="86%" stopColor="#24272c" />
        <stop offset="100%" stopColor="#08090a" />
      </radialGradient>
      <linearGradient id="ws-metal" x1="0" y1="0" x2="1" y2="1">
        <stop offset="0%" stopColor="#f4f6f8" />
        <stop offset="45%" stopColor="#9aa3ad" />
        <stop offset="70%" stopColor="#d9dee3" />
        <stop offset="100%" stopColor="#6b737c" />
      </linearGradient>
      <radialGradient id="ws-disc" cx="50%" cy="50%" r="50%">
        <stop offset="40%" stopColor="#5b6067" />
        <stop offset="75%" stopColor="#a9afb6" />
        <stop offset="92%" stopColor="#80868d" />
        <stop offset="100%" stopColor="#4a4f55" />
      </radialGradient>
      <radialGradient id="ws-heat" cx="50%" cy="50%" r="50%">
        <stop offset="45%" stopColor="#ff6a00" stopOpacity="0" />
        <stop offset="80%" stopColor="#ff6a00" stopOpacity="0.85" />
        <stop offset="100%" stopColor="#ffb703" stopOpacity="0.9" />
      </radialGradient>
      <linearGradient id="ws-caliper" x1="0" y1="0" x2="0" y2="1">
        <stop offset="0%" stopColor="#ff4d4d" />
        <stop offset="100%" stopColor="#a4161a" />
      </linearGradient>
      <radialGradient id="ws-hub" cx="40%" cy="35%" r="70%">
        <stop offset="0%" stopColor="#6a7079" />
        <stop offset="100%" stopColor="#2a2e34" />
      </radialGradient>
      <radialGradient id="ws-floor" cx="50%" cy="50%" r="50%">
        <stop offset="0%" stopColor="#3b82f6" stopOpacity="0.35" />
        <stop offset="100%" stopColor="#3b82f6" stopOpacity="0" />
      </radialGradient>
    </defs>
  )
}

const ring = (count: number, r: number, render: (x: number, y: number, a: number, i: number) => React.ReactNode) =>
  Array.from({ length: count }, (_, i) => {
    const a = (i / count) * Math.PI * 2
    // rounded: Math.cos/sin differ in the last digits between the server and the browser (hydration)
    const round = (v: number) => Math.round(v * 100) / 100
    return render(round(Math.cos(a) * r), round(Math.sin(a) * r), round((a * 180) / Math.PI), i)
  })

function Tyre() {
  return (
    <>
      <circle r="172" fill="none" stroke="url(#ws-tyre)" strokeWidth="58" />
      {/* tread blocks */}
      <circle r="198" fill="none" stroke="#1d2024" strokeWidth="8" strokeDasharray="10 9" />
      <circle r="146" fill="none" stroke="#2c3036" strokeWidth="2" />
      <circle r="190" fill="none" stroke="#33373d" strokeWidth="1.5" />
    </>
  )
}

function Rim() {
  return (
    <>
      <circle r="140" fill="none" stroke="url(#ws-metal)" strokeWidth="9" />
      <circle r="131" fill="none" stroke="#5c636b" strokeWidth="2" />
      {ring(5, 0, (_x, _y, a, i) => (
        <g key={i} transform={`rotate(${a})`}>
          <path d="M 34 -11 L 132 -17 L 132 17 L 34 11 Z" fill="url(#ws-metal)" stroke="#6b737c" strokeWidth="1" />
        </g>
      ))}
      <circle r="38" fill="url(#ws-metal)" stroke="#6b737c" strokeWidth="1.5" />
      <circle r="13" fill="#1f2328" />
      {ring(5, 24, (x, y, _a, i) => <circle key={i} cx={x} cy={y} r="4" fill="#2a2e34" />)}
    </>
  )
}

function Disc({ heatRef }: { heatRef: React.Ref<SVGCircleElement> }) {
  return (
    <>
      <circle r="120" fill="url(#ws-disc)" />
      <circle r="120" fill="none" stroke="#3d4248" strokeWidth="2" />
      {/* drilled and slotted friction ring */}
      {ring(18, 98, (x, y, _a, i) => <circle key={`a${i}`} cx={x} cy={y} r="3.6" fill="#2b2f34" />)}
      {ring(18, 84, (x, y, _a, i) => <circle key={`b${i}`} cx={x * 0.99} cy={y * 0.99} r="3.2" fill="#2b2f34" transform="rotate(10)" />)}
      {ring(6, 0, (_x, _y, a, i) => (
        <path key={`s${i}`} d="M 70 0 Q 92 8 112 4" stroke="#3a3f45" strokeWidth="3" fill="none" transform={`rotate(${a + 30})`} />
      ))}
      <circle r="60" fill="#3f444b" stroke="#2b2f34" strokeWidth="2" />
      <circle ref={heatRef} r="120" fill="url(#ws-heat)" opacity="0" />
    </>
  )
}

function Caliper() {
  // sits on the top right of the disc and does not turn
  return (
    <g transform="rotate(-40)">
      <path d="M 82 -40 A 92 92 0 0 1 82 40 L 138 58 A 150 150 0 0 0 138 -58 Z" fill="url(#ws-caliper)" stroke="#7a0f12" strokeWidth="2" />
      <rect x="100" y="-14" width="26" height="28" rx="5" fill="#ffffff" opacity="0.9" />
      <text x="113" y="5" textAnchor="middle" fontSize="13" fontWeight="700" fill="#a4161a" fontFamily="Inter, sans-serif">VG</text>
    </g>
  )
}

function Hub() {
  return (
    <>
      <circle r="66" fill="url(#ws-hub)" stroke="#1d2024" strokeWidth="2" />
      <circle r="44" fill="none" stroke="#4c525a" strokeWidth="3" />
      {ring(5, 34, (x, y, _a, i) => <circle key={i} cx={x} cy={y} r="6" fill="#c8ced4" stroke="#5c636b" />)}
      <circle r="16" fill="#9aa3ad" />
    </>
  )
}

// ------------------------------------------------------------------ component

/**
 * Apple-style story: the section is pinned while you scroll and the wheel turns, comes apart into tyre, rim,
 * brake disc, caliper and hub, the disc glows under braking, the parts become the lines of an estimate and the
 * wheel goes back together. Everything is driven by the scroll position (transform and opacity only); the server
 * renders the first frame, so there is nothing to hydrate differently. With "reduce motion" the wheel stays apart
 * and every step is shown.
 */
export default function WheelStory({ t }: { t: WheelStoryText }) {
  const sectionRef = useRef<HTMLElement>(null)
  const partRefs = {
    tyre: useRef<SVGGElement>(null),
    rim: useRef<SVGGElement>(null),
    disc: useRef<SVGGElement>(null),
    hub: useRef<SVGGElement>(null),
  }
  const spinRefs = [useRef<SVGGElement>(null), useRef<SVGGElement>(null), useRef<SVGGElement>(null), useRef<SVGGElement>(null)]
  const heatRef = useRef<SVGCircleElement>(null)
  const labelsRef = useRef<SVGGElement>(null)
  const captionRefs = [useRef<HTMLDivElement>(null), useRef<HTMLDivElement>(null), useRef<HTMLDivElement>(null), useRef<HTMLDivElement>(null)]
  const estimateRef = useRef<HTMLDivElement>(null)
  const lineRefs = [useRef<HTMLLIElement>(null), useRef<HTMLLIElement>(null), useRef<HTMLLIElement>(null), useRef<HTMLLIElement>(null)]
  const signedRef = useRef<HTMLDivElement>(null)
  const progressRef = useRef<HTMLDivElement>(null)
  const invoiceRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    const section = sectionRef.current
    if (!section) return
    const reduce = window.matchMedia('(prefers-reduced-motion: reduce)')

    const apply = (p: number) => {
      for (const name of Object.keys(PARTS) as PartName[]) partRefs[name].current?.setAttribute('transform', partTransform(name, p))
      for (const r of spinRefs) r.current?.setAttribute('transform', spin(p))
      heatRef.current?.setAttribute('opacity', String(heatOpacity(p)))
      labelsRef.current?.setAttribute('opacity', String(labelOpacity(p)))
      captionRefs.forEach((r, i) => {
        const o = captionOpacity(p, i)
        if (r.current) {
          r.current.style.opacity = String(o)
          r.current.style.transform = `translateY(${(1 - o) * 18}px)`
        }
      })
      if (estimateRef.current) {
        const o = estimateOpacity(p)
        estimateRef.current.style.opacity = String(o)
        estimateRef.current.style.transform = `translateY(${(1 - o) * 24}px) scale(${0.96 + 0.04 * o})`
      }
      lineRefs.forEach((r, i) => { if (r.current) r.current.style.opacity = String(lineOpacity(p, i)) })
      if (signedRef.current) signedRef.current.style.opacity = String(signedOpacity(p))
      if (progressRef.current) progressRef.current.style.transform = `scaleX(${p})`
      if (invoiceRef.current) {
        const o = invoiceOpacity(p)
        invoiceRef.current.style.opacity = String(o)
        invoiceRef.current.style.transform = `translate(-50%, ${(1 - o) * 16}px)`
      }
    }

    if (reduce.matches) {
      // a still picture of the wheel taken apart, the steps are shown one below the other (CSS)
      section.dataset.still = 'true'
      apply(0.45)
      return
    }

    let frame = 0
    const update = () => {
      frame = 0
      const rect = section.getBoundingClientRect()
      const length = rect.height - window.innerHeight
      apply(length > 0 ? clamp(-rect.top / length) : 0)
    }
    const schedule = () => { if (!frame) frame = requestAnimationFrame(update) }
    update()
    window.addEventListener('scroll', schedule, { passive: true })
    window.addEventListener('resize', schedule)
    return () => {
      window.removeEventListener('scroll', schedule)
      window.removeEventListener('resize', schedule)
      if (frame) cancelAnimationFrame(frame)
    }
    // the refs are stable; the effect runs once
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  const label = (part: PartName, text: string, side: 'up' | 'down', r: number) => {
    const { x, scale } = PARTS[part]
    const px = CX + x
    const edge = r * scale
    const y1 = side === 'up' ? CY - edge - 8 : CY + edge + 8
    const y2 = side === 'up' ? y1 - 34 : y1 + 34
    return (
      <g key={part}>
        <line x1={px} y1={y1} x2={px} y2={y2} stroke="#60a5fa" strokeWidth="1.5" strokeDasharray="3 4" />
        <circle cx={px} cy={y1} r="3" fill="#60a5fa" />
        <text x={px} y={side === 'up' ? y2 - 10 : y2 + 22} textAnchor="middle" fontSize="20" fontWeight="600" fill="#e5e7eb"
          fontFamily="Inter, 'PingFang SC', 'Microsoft YaHei', sans-serif">{text}</text>
      </g>
    )
  }

  return (
    <section ref={sectionRef} className="wheel-story relative h-[420vh] bg-black" aria-label={t.story.map(s => s.title).join(' ')}>
      <div className="sticky top-0 flex h-screen flex-col overflow-hidden">
        {/* progress of the story */}
        <div className="absolute inset-x-0 top-0 z-10 h-0.5 bg-white/5">
          <div ref={progressRef} className="h-full origin-left scale-x-0 bg-gradient-to-r from-blue-500 to-indigo-400" />
        </div>

        <div className="mx-auto grid h-full w-full max-w-7xl grid-rows-[auto_1fr] items-center gap-4 px-6 pt-20 pb-8 lg:grid-cols-[minmax(0,5fr)_minmax(0,7fr)] lg:grid-rows-1 lg:gap-10 lg:px-8 lg:pt-16">
          {/* captions: one at a time while scrolling */}
          <div className="story-captions relative min-h-44 lg:min-h-72">
            {t.story.map((s, i) => (
              <div key={s.kicker} ref={captionRefs[i]} className="story-caption absolute inset-x-0 top-0 will-change-transform"
                style={{ opacity: i === 0 ? 1 : 0 }}>
                <p className="text-sm font-semibold tracking-wide text-blue-400 uppercase">{String(i + 1).padStart(2, '0')} · {s.kicker}</p>
                <h2 className="mt-3 text-3xl font-semibold tracking-tight text-balance text-white sm:text-4xl lg:text-5xl">{s.title}</h2>
                <p className="mt-4 max-w-md text-base text-pretty text-slate-400 sm:text-lg">{s.text}</p>
              </div>
            ))}
          </div>

          {/* the wheel */}
          <div className="relative h-full min-h-0">
            <svg viewBox="0 0 1040 600" className="story-wheel absolute inset-y-0 left-1/2 h-full w-[135%] max-w-none -translate-x-1/2 sm:w-[115%] lg:w-full" role="img" aria-label={Object.values(t.parts).join(', ')}>
              <Defs />
              <ellipse cx={CX} cy={CY + 230} rx="420" ry="40" fill="url(#ws-floor)" />
              <g ref={partRefs.hub} transform={partTransform('hub', 0)}>
                <g ref={spinRefs[0]} transform={spin(0)}><Hub /></g>
              </g>
              <g ref={partRefs.disc} transform={partTransform('disc', 0)}>
                <g ref={spinRefs[1]} transform={spin(0)}><Disc heatRef={heatRef} /></g>
                <Caliper />
              </g>
              <g ref={partRefs.rim} transform={partTransform('rim', 0)}>
                <g ref={spinRefs[2]} transform={spin(0)}><Rim /></g>
              </g>
              <g ref={partRefs.tyre} transform={partTransform('tyre', 0)}>
                <g ref={spinRefs[3]} transform={spin(0)}><Tyre /></g>
              </g>
              <g ref={labelsRef} opacity="0">
                {label('tyre', t.parts.tyre, 'up', 200)}
                {label('rim', t.parts.rim, 'down', 142)}
                {label('disc', t.parts.disc, 'up', 122)}
                {label('hub', t.parts.hub, 'down', 68)}
                <g>
                  <line x1={CX + PARTS.disc.x + 40} y1={CY - 70} x2={CX + PARTS.disc.x + 110} y2={CY - 150} stroke="#f87171" strokeWidth="1.5" strokeDasharray="3 4" />
                  <circle cx={CX + PARTS.disc.x + 40} cy={CY - 70} r="3" fill="#f87171" />
                  <text x={CX + PARTS.disc.x + 116} y={CY - 156} fontSize="20" fontWeight="600" fill="#fecaca"
                    fontFamily="Inter, 'PingFang SC', 'Microsoft YaHei', sans-serif">{t.parts.caliper}</text>
                </g>
              </g>
            </svg>

            {/* the parts become an estimate */}
            <div ref={estimateRef} style={{ opacity: 0 }}
              className="story-estimate absolute right-0 bottom-2 w-full max-w-xs rounded-2xl bg-slate-900/85 p-4 shadow-2xl ring-1 ring-white/10 backdrop-blur will-change-transform sm:right-4 sm:p-5">
              <div className="flex items-center justify-between">
                <p className="text-sm font-semibold text-white">{t.estimateTitle}</p>
                <p className="font-mono text-[0.65rem] text-blue-300">OF_TF_2019_HC</p>
              </div>
              <ul className="mt-3 space-y-1.5 text-sm">
                {t.estimateLines.map((l, i) => (
                  <li key={l.name} ref={lineRefs[i]} style={{ opacity: 0 }} className="flex justify-between gap-3 text-slate-300">
                    <span className="truncate">{l.name}</span><span className="text-white tabular-nums">{l.price}</span>
                  </li>
                ))}
              </ul>
              <div className="mt-3 space-y-1 border-t border-white/10 pt-2 text-sm">
                <div className="flex justify-between text-slate-400"><span>{t.estimateTax}</span><span className="tabular-nums">{t.estimateTaxAmount}</span></div>
                <div className="flex justify-between font-semibold text-white"><span>{t.estimateTotal}</span><span className="tabular-nums">{t.estimateTotalAmount}</span></div>
              </div>
              <div ref={signedRef} style={{ opacity: 0 }} className="mt-3 inline-flex items-center gap-1.5 rounded-full bg-emerald-500/15 px-2.5 py-1 text-xs font-medium text-emerald-300 ring-1 ring-emerald-400/30 ring-inset">
                <svg viewBox="0 0 16 16" className="size-3.5" aria-hidden="true"><path d="M3 8.5l3 3 7-7" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" /></svg>
                {t.signed}
              </div>
            </div>

            {/* back together: invoiced */}
            <div ref={invoiceRef} style={{ opacity: 0, transform: 'translate(-50%, 16px)' }}
              className="story-invoice absolute bottom-[14%] left-1/2 inline-flex items-center gap-2 rounded-full bg-violet-500/15 px-4 py-2 text-sm font-medium whitespace-nowrap text-violet-200 ring-1 ring-violet-400/30 ring-inset backdrop-blur will-change-transform">
              <svg viewBox="0 0 16 16" className="size-4" aria-hidden="true"><path d="M3 8.5l3 3 7-7" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" /></svg>
              {t.invoiced}
            </div>
          </div>
        </div>
      </div>
    </section>
  )
}
