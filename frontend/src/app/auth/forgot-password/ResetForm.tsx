'use client'
import { useActionState } from 'react'
import { resetPassword } from './actions'
import { inputClass, linkClass, primaryButtonClass } from '../_components/AuthShell';

export default function ResetForm() {
  const [state, action, pending] = useActionState(resetPassword, { step: 'request' });

  if (state.step === 'done') {
    return (
      <div className="space-y-6">
        <p className="rounded-md bg-green-50 p-3 text-sm text-green-700">Your password was changed. You can sign in now.</p>
        <a href="/auth/login" className={primaryButtonClass}>Sign in</a>
      </div>
    );
  }

  return (
    <form action={action} className="space-y-6">
      {state.error && <p className="rounded-md bg-red-50 p-3 text-sm text-red-700">{state.error}</p>}
      {state.step === 'request' ? (
        <>
          <p className="text-sm/6 text-gray-700">Enter your username or email address. If the account has an email address, we send a code to it.</p>
          <div>
            <label htmlFor="login" className="block text-sm/6 font-medium text-gray-900">Username or email</label>
            <input id="login" name="login" type="text" required autoComplete="username" className={"mt-2 " + inputClass} />
          </div>
          <button type="submit" disabled={pending} className={primaryButtonClass}>Send code</button>
        </>
      ) : (
        <>
          <p className="text-sm/6 text-gray-700">If the account exists, a 6 digit code was sent to its email address. It expires in a few minutes.</p>
          <div>
            <label htmlFor="code" className="block text-sm/6 font-medium text-gray-900">Code</label>
            <input id="code" name="code" type="text" inputMode="numeric" autoComplete="one-time-code" required className={"mt-2 tracking-widest " + inputClass} />
          </div>
          <div>
            <label htmlFor="newPassword" className="block text-sm/6 font-medium text-gray-900">New password</label>
            <input id="newPassword" name="newPassword" type="password" required minLength={10} autoComplete="new-password" className={"mt-2 " + inputClass} />
          </div>
          <div>
            <label htmlFor="confirmPassword" className="block text-sm/6 font-medium text-gray-900">Confirm new password</label>
            <input id="confirmPassword" name="confirmPassword" type="password" required minLength={10} autoComplete="new-password" className={"mt-2 " + inputClass} />
          </div>
          <button type="submit" disabled={pending} className={primaryButtonClass}>Change password</button>
        </>
      )}
      <p className="text-center text-sm"><a href="/auth/login" className={linkClass}>Back to sign in</a></p>
    </form>
  );
}
