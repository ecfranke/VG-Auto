'use client'

import { ActionState, deleteClientOrVehicle } from '../actions'
import { useFormAction } from './useFormAction'

/** Delete button of a client or vehicle row; disabled while it is in use, the API's reason is shown when it refuses. */
export default function DeleteRowButton({ kind, id, label, itemLabel, confirmText, returnTo, inUse, inUseText }: {
  kind: 'clients' | 'vehicles', id: string, label: string, itemLabel: string, confirmText: string, returnTo: string, inUse: boolean, inUseText: string
}) {
  const [state, onSubmit, pending] = useFormAction<ActionState>(deleteClientOrVehicle, {})
  return (
    <form onSubmit={e => { if (window.confirm(`${itemLabel}\n\n${confirmText}`)) onSubmit(e); else e.preventDefault() }}
      className="flex flex-col items-end gap-1">
      <input type="hidden" name="kind" value={kind} />
      <input type="hidden" name="id" value={id} />
      <input type="hidden" name="label" value={itemLabel} />
      <input type="hidden" name="returnTo" value={returnTo} />
      <button type="submit" disabled={pending || inUse} title={inUse ? inUseText : undefined}
        className="rounded-md px-2.5 py-1 text-sm font-semibold text-red-600 ring-1 ring-red-600/30 ring-inset hover:bg-red-50 disabled:cursor-not-allowed disabled:text-gray-400 disabled:ring-gray-300 disabled:hover:bg-transparent dark:hover:bg-red-500/10">
        {inUse ? inUseText : label}
      </button>
      {state.error && <span className="max-w-xs text-right text-xs text-red-600">{state.error}</span>}
    </form>
  )
}
