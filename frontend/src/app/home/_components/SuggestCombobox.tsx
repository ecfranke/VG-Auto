'use client'

import { useMemo, useState } from 'react'
import { Combobox, ComboboxButton, ComboboxInput, ComboboxOption, ComboboxOptions } from '@headlessui/react'
import { CheckIcon, ChevronUpDownIcon } from '@heroicons/react/20/solid'
import clsx from 'clsx'

const MAX_OPTIONS = 60

/**
 * A text field with suggestions: choose one from the list or type any other value. The value is submitted with the
 * form as `name`, also when it was typed and not chosen.
 */
export default function SuggestCombobox({ id, name, value, onChange, options, placeholder, emptyHint }: {
  id?: string,
  name: string,
  value: string,
  onChange: (value: string) => void,
  options: string[],
  placeholder?: string,
  /** shown in the list when there are no suggestions */
  emptyHint?: string,
}) {
  const [query, setQuery] = useState('')

  const filtered = useMemo(() => {
    const q = query.trim().toLowerCase()
    if (!q) return options.slice(0, MAX_OPTIONS)
    const starts = options.filter(o => o.toLowerCase().startsWith(q))
    const contains = options.filter(o => !o.toLowerCase().startsWith(q) && o.toLowerCase().includes(q))
    return [...starts, ...contains].slice(0, MAX_OPTIONS)
  }, [options, query])

  const typed = query.trim()
  const custom = typed && !options.some(o => o.toLowerCase() === typed.toLowerCase()) ? typed : null

  return (
    <>
      <input type="hidden" name={name} value={value} />
      <Combobox value={value} onChange={(v: string | null) => { onChange(v ?? ''); setQuery('') }} immediate>
        <div className="relative mt-2">
          <ComboboxInput id={id} autoComplete="off" placeholder={placeholder}
            className="block w-full rounded-md bg-surface py-1.5 pr-10 pl-3 text-base text-gray-900 outline-1 -outline-offset-1 outline-gray-300 placeholder:text-gray-400 focus:outline-2 focus:-outline-offset-2 focus:outline-primary sm:text-sm/6"
            displayValue={(v: string) => v}
            onChange={e => { setQuery(e.target.value); onChange(e.target.value) }}
            onBlur={() => setQuery('')} />
          <ComboboxButton className="absolute inset-y-0 right-0 flex items-center rounded-r-md px-2 focus:outline-hidden" aria-label="Show suggestions">
            <ChevronUpDownIcon className="size-5 text-gray-400" aria-hidden="true" />
          </ComboboxButton>
          <ComboboxOptions modal={false}
            className="absolute z-10 mt-1 max-h-60 w-full overflow-auto rounded-md bg-surface py-1 text-base shadow-lg ring-1 ring-black/5 focus:outline-hidden empty:invisible sm:text-sm dark:ring-white/10">
            {custom && (
              <ComboboxOption value={custom} className="cursor-default px-3 py-2 text-gray-900 select-none data-focus:bg-primary data-focus:text-white">
                Use “{custom}”
              </ComboboxOption>
            )}
            {filtered.map(option => (
              <ComboboxOption key={option} value={option}
                className="group relative cursor-default py-2 pr-9 pl-3 text-gray-900 select-none data-focus:bg-primary data-focus:text-white">
                <span className={clsx('block truncate', option === value && 'font-semibold')}>{option}</span>
                {option === value && (
                  <span className="absolute inset-y-0 right-0 flex items-center pr-3 text-link group-data-focus:text-white">
                    <CheckIcon className="size-5" aria-hidden="true" />
                  </span>
                )}
              </ComboboxOption>
            ))}
            {!custom && filtered.length === 0 && emptyHint && (
              <div className="px-3 py-2 text-sm text-gray-500">{emptyHint}</div>
            )}
          </ComboboxOptions>
        </div>
      </Combobox>
    </>
  )
}
