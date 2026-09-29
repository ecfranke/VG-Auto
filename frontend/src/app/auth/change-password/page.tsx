'use client'
import Image from 'next/image';
import { useActionState } from 'react'
import { changeInitialPassword } from './actions'

const inputClass = "block w-full rounded-md bg-surface px-3 py-1.5 text-base text-gray-900 outline-1 -outline-offset-1 outline-gray-300 placeholder:text-gray-400 focus:outline-2 focus:-outline-offset-2 focus:outline-primary text-sm/6";

export default function ChangePasswordPage() {
  const [state, action, pending] = useActionState(changeInitialPassword, { error: '' });

  return (
    <div className="bg-surface flex min-h-full flex-1">
      <div className="flex flex-1 flex-col justify-center px-4 py-12 sm:px-6 lg:flex-none lg:px-20 xl:px-24">
        <div className="mx-auto w-full max-w-sm lg:w-96">
          <Image alt="Logo" width="50" height="50" className="h-10 w-auto" src="/logo.png" />
          <h2 className="mt-8 text-2xl/9 font-bold tracking-tight text-gray-900">Choose a new password</h2>
          <p className="mt-2 text-sm/6 text-gray-500">
            Your password must be changed before you can continue. Use at least 10 characters.
          </p>
          <form action={action} className="mt-10 space-y-6">
            {state?.error && <p className="text-sm text-red-600">{state.error}</p>}
            <div>
              <label htmlFor="currentPassword" className="block text-sm/6 font-medium text-gray-900">Current password</label>
              <input id="currentPassword" name="currentPassword" type="password" required autoComplete="current-password" className={"mt-2 " + inputClass} />
            </div>
            <div>
              <label htmlFor="newPassword" className="block text-sm/6 font-medium text-gray-900">New password</label>
              <input id="newPassword" name="newPassword" type="password" required minLength={10} autoComplete="new-password" className={"mt-2 " + inputClass} />
            </div>
            <div>
              <label htmlFor="confirmPassword" className="block text-sm/6 font-medium text-gray-900">Confirm new password</label>
              <input id="confirmPassword" name="confirmPassword" type="password" required minLength={10} autoComplete="new-password" className={"mt-2 " + inputClass} />
            </div>
            <button type="submit" disabled={pending}
              className="flex w-full justify-center rounded-md bg-primary px-3 py-1.5 text-sm/6 font-semibold text-white shadow-xs hover:bg-primary-hover disabled:opacity-50">
              Change password
            </button>
            <p className="text-center text-sm"><a href="/home/logout" className="text-link hover:text-link-hover">Sign out</a></p>
          </form>
        </div>
      </div>
    </div>
  )
}
