'use client'

import { startTransition, useActionState } from 'react'

/**
 * useActionState for a form submitted with onSubmit instead of the action attribute: React resets a form after its
 * action, which would also clear what was typed when the action returns an error.
 */
export function useFormAction<S>(action: (state: Awaited<S>, form: FormData) => S | Promise<S>, initial: Awaited<S>) {
  const [state, dispatch, pending] = useActionState(action, initial)
  const onSubmit = (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault()
    const form = new FormData(e.currentTarget)
    startTransition(() => dispatch(form))
  }
  return [state, onSubmit, pending] as const
}
