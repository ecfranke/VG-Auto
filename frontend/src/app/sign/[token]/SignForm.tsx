'use client'

import { useEffect, useRef, useState, useTransition } from 'react'
import { useRouter } from 'next/navigation'
import { signEstimate } from './actions'

/** A box to sign in with the mouse, a pen or a finger. */
function SignaturePad({ onChange }: { onChange: (dataUrl: string | null) => void }) {
  const canvasRef = useRef<HTMLCanvasElement>(null)
  const drawing = useRef(false)
  const [empty, setEmpty] = useState(true)

  // the drawing surface matches the box on screen and the display's pixel density
  useEffect(() => {
    const canvas = canvasRef.current!
    const ratio = Math.max(1, Math.min(window.devicePixelRatio || 1, 2))
    canvas.width = canvas.offsetWidth * ratio
    canvas.height = canvas.offsetHeight * ratio
    const context = canvas.getContext('2d')!
    context.scale(ratio, ratio)
    context.lineWidth = 2.2
    context.lineCap = 'round'
    context.lineJoin = 'round'
    context.strokeStyle = '#111827'
  }, [])

  function point(e: React.PointerEvent<HTMLCanvasElement>) {
    const rect = e.currentTarget.getBoundingClientRect()
    return { x: e.clientX - rect.left, y: e.clientY - rect.top }
  }

  function start(e: React.PointerEvent<HTMLCanvasElement>) {
    e.currentTarget.setPointerCapture(e.pointerId)
    drawing.current = true
    const context = e.currentTarget.getContext('2d')!
    const { x, y } = point(e)
    context.beginPath()
    context.moveTo(x, y)
    context.lineTo(x + 0.1, y + 0.1)
    context.stroke()
  }

  function move(e: React.PointerEvent<HTMLCanvasElement>) {
    if (!drawing.current) return
    const context = e.currentTarget.getContext('2d')!
    const { x, y } = point(e)
    context.lineTo(x, y)
    context.stroke()
  }

  function end(e: React.PointerEvent<HTMLCanvasElement>) {
    if (!drawing.current) return
    drawing.current = false
    setEmpty(false)
    onChange(e.currentTarget.toDataURL('image/png'))
  }

  function clear() {
    const canvas = canvasRef.current!
    canvas.getContext('2d')!.clearRect(0, 0, canvas.width, canvas.height)
    setEmpty(true)
    onChange(null)
  }

  return (
    <div>
      <div className="relative">
        <canvas ref={canvasRef} aria-label="Signature" role="img"
          onPointerDown={start} onPointerMove={move} onPointerUp={end} onPointerCancel={end}
          className="block h-44 w-full cursor-crosshair touch-none rounded-md bg-white ring-1 ring-gray-300 ring-inset" />
        {empty && <span className="pointer-events-none absolute inset-0 flex items-center justify-center text-sm text-gray-400">Sign here</span>}
        <span className="pointer-events-none absolute inset-x-6 bottom-8 border-b border-dashed border-gray-300" />
      </div>
      <div className="mt-2 flex justify-end">
        <button type="button" onClick={clear} className="text-sm font-medium text-link hover:text-link-hover">Clear</button>
      </div>
    </div>
  )
}

export default function SignForm({ token, defaultName }: { token: string, defaultName: string }) {
  const router = useRouter()
  const [name, setName] = useState(defaultName)
  const [accepted, setAccepted] = useState(false)
  const [signature, setSignature] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [pending, start] = useTransition()

  function submit(e: React.FormEvent) {
    e.preventDefault()
    setError(null)
    if (!signature) {
      setError('Please sign in the box.')
      return
    }
    start(async () => {
      const result = await signEstimate(token, name, accepted, signature)
      if (result.error) setError(result.error)
      else router.refresh()
    })
  }

  return (
    <form onSubmit={submit} className="space-y-5">
      {error && <div role="alert" className="rounded-md bg-red-50 p-3 text-sm text-red-800 ring-1 ring-red-600/20 ring-inset">{error}</div>}
      <div className="max-w-md">
        <label htmlFor="signer" className="block text-sm/6 font-medium text-gray-900">Full name</label>
        <input id="signer" value={name} onChange={e => setName(e.target.value)} required maxLength={200} autoComplete="name"
          className="mt-1 block w-full rounded-md bg-surface px-3 py-1.5 text-sm text-gray-900 outline-1 -outline-offset-1 outline-gray-300 focus:outline-2 focus:-outline-offset-2 focus:outline-primary" />
      </div>
      <div>
        <span className="block text-sm/6 font-medium text-gray-900">Signature</span>
        <div className="mt-1 max-w-xl">
          <SignaturePad onChange={setSignature} />
        </div>
      </div>
      <label className="flex items-start gap-x-2 text-sm text-gray-900">
        <input type="checkbox" checked={accepted} onChange={e => setAccepted(e.target.checked)} required className="mt-0.5 size-4 accent-primary" />
        <span>I have read this estimate and accept it.</span>
      </label>
      <button type="submit" disabled={pending}
        className="rounded-md bg-primary px-4 py-2 text-sm font-semibold text-white shadow-xs hover:bg-primary-hover disabled:opacity-50">
        {pending ? 'Signing…' : 'Sign estimate'}
      </button>
    </form>
  )
}
