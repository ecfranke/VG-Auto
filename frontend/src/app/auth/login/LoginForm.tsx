'use client'
import { useActionState } from 'react'
import { authenticate, LoginState } from './authenticate'
import { inputClass, linkClass, primaryButtonClass } from '../_components/AuthShell';

function MicrosoftLogo() {
  return (
    <svg viewBox="0 0 23 23" aria-hidden="true" className="h-5 w-5">
      <path fill="#f35325" d="M1 1h10v10H1z" />
      <path fill="#81bc06" d="M12 1h10v10H12z" />
      <path fill="#05a6f0" d="M1 12h10v10H1z" />
      <path fill="#ffba08" d="M12 12h10v10H12z" />
    </svg>
  );
}

export default function LoginForm({ initialState, passwordReset, microsoft }: {
  initialState: LoginState,
  passwordReset: boolean,
  microsoft: boolean,
}) {
  const [state, action, pending] = useActionState(authenticate, initialState);

  return (
    <>
      {state.error && <p className="mb-4 rounded-md bg-red-50 p-3 text-sm text-red-700">{state.error}</p>}
      {state.info && <p className="mb-4 rounded-md bg-green-50 p-3 text-sm text-green-700">{state.info}</p>}

      {state.step === 'code' ? (
        <form action={action} className="space-y-6">
          <p className="text-sm/6 text-gray-700">
            We sent a 6 digit code to <span className="font-semibold">{state.emailHint ?? 'your email address'}</span>.
            Enter it to continue.
          </p>
          <div>
            <label htmlFor="code" className="block text-sm/6 font-medium text-gray-900">Code</label>
            <input id="code" name="code" type="text" inputMode="numeric" autoComplete="one-time-code" pattern="[0-9 ]{6,7}"
              required autoFocus className={"mt-2 tracking-widest " + inputClass} />
          </div>
          <button type="submit" name="intent" value="code" disabled={pending} className={primaryButtonClass}>Verify</button>
          <div className="flex justify-between text-sm/6">
            <button type="submit" name="intent" value="resend" formNoValidate disabled={pending} className={linkClass}>Send a new code</button>
            <a href="/auth/login" className={linkClass}>Start over</a>
          </div>
        </form>
      ) : (
        <form action={action} className="space-y-6">
          <div>
            <label htmlFor="username" className="block text-sm/6 font-medium text-gray-900">Username</label>
            <input id="username" name="username" type="text" required autoComplete="username" className={"mt-2 " + inputClass} />
          </div>
          <div>
            <label htmlFor="password" className="block text-sm/6 font-medium text-gray-900">Password</label>
            <input id="password" name="password" type="password" required autoComplete="current-password" className={"mt-2 " + inputClass} />
          </div>
          {passwordReset && (
            <div className="flex justify-end text-sm/6">
              <a href="/auth/forgot-password" className={linkClass}>Forgot password?</a>
            </div>
          )}
          <button type="submit" name="intent" value="password" disabled={pending} className={primaryButtonClass}>Sign in</button>
        </form>
      )}

      {microsoft && state.step === 'password' && (
        <div className="mt-10">
          <div className="relative">
            <div aria-hidden="true" className="absolute inset-0 flex items-center">
              <div className="w-full border-t border-gray-200" />
            </div>
            <div className="relative flex justify-center text-sm/6 font-medium">
              <span className="bg-white px-6 text-gray-900">Or continue with</span>
            </div>
          </div>
          <div className="mt-6">
            <a href="/auth/microsoft/start"
              className="flex w-full items-center justify-center gap-3 rounded-md bg-white px-3 py-2 text-sm font-semibold text-gray-900 ring-1 shadow-xs ring-gray-300 ring-inset hover:bg-gray-50">
              <MicrosoftLogo />
              <span className="text-sm/6 font-semibold">Microsoft</span>
            </a>
          </div>
        </div>
      )}
    </>
  );
}
