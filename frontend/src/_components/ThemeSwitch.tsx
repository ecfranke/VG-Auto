'use client'

import { useState } from 'react'
import clsx from 'clsx'
import { ComputerDesktopIcon, MoonIcon, SunIcon } from '@heroicons/react/20/solid'

export type Theme = 'system' | 'light' | 'dark'
export const THEME_COOKIE = 'theme'

const options: { value: Theme, label: string, Icon: typeof SunIcon }[] = [
  { value: 'system', label: 'System', Icon: ComputerDesktopIcon },
  { value: 'light', label: 'Light', Icon: SunIcon },
  { value: 'dark', label: 'Dark', Icon: MoonIcon },
]

/** Light, dark or the system's choice; saved for a year, applied at once (the root layout reads it on the server). */
export default function ThemeSwitch({ initial, labels, onDark = true, className }: {
  initial: Theme,
  labels?: Partial<Record<Theme, string>>,
  /** on the always dark sidebar */
  onDark?: boolean,
  className?: string,
}) {
  const [theme, setTheme] = useState<Theme>(initial)

  function choose(value: Theme) {
    setTheme(value)
    document.cookie = `${THEME_COOKIE}=${value}; path=/; max-age=${60 * 60 * 24 * 365}; samesite=lax`
    const root = document.documentElement
    root.classList.remove('light', 'dark')
    if (value !== 'system') root.classList.add(value)
  }

  return (
    <div role="radiogroup" aria-label="Theme" className={clsx('inline-flex rounded-lg p-0.5 ring-1 ring-inset',
      onDark ? 'bg-white/5 ring-white/10' : 'bg-gray-100 ring-gray-200', className)}>
      {options.map(({ value, label, Icon }) => {
        const active = theme === value
        const text = labels?.[value] ?? label
        return (
          <button key={value} type="button" role="radio" aria-checked={active} title={text} onClick={() => choose(value)}
            className={clsx('flex items-center rounded-md px-2 py-1',
              onDark
                ? active ? 'bg-white/15 text-white' : 'text-slate-400 hover:text-white'
                : active ? 'bg-surface text-gray-900 shadow-xs' : 'text-gray-500 hover:text-gray-900')}>
            <Icon className="size-4" aria-hidden="true" />
            <span className="sr-only">{text}</span>
          </button>
        )
      })}
    </div>
  )
}
