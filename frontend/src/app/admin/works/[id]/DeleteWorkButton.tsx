'use client'

import { ActionState, deleteWork } from '../../actions'
import { Alert, SubmitButton } from '../../_components/Fields'
import { useFormAction } from '../../_components/useFormAction'

export default function DeleteWorkButton({ workId, code, label, confirmText }: { workId: string, code: string, label: string, confirmText: string }) {
  const [state, onSubmit, pending] = useFormAction<ActionState>(deleteWork, {})
  return (
    <form onSubmit={e => { if (window.confirm(`${code}\n\n${confirmText}`)) onSubmit(e); else e.preventDefault() }} className="space-y-3">
      <input type="hidden" name="workId" value={workId} />
      <input type="hidden" name="code" value={code} />
      {state.error && <Alert kind="error">{state.error}</Alert>}
      <SubmitButton pending={pending} danger>{label}</SubmitButton>
    </form>
  )
}
