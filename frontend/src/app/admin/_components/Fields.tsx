import clsx from 'clsx'

const inputClass = 'block w-full rounded-md bg-white px-3 py-1.5 text-sm text-gray-900 outline-1 -outline-offset-1 outline-gray-300 placeholder:text-gray-400 focus:outline-2 focus:-outline-offset-2 focus:outline-indigo-600 disabled:bg-gray-50 disabled:text-gray-500'

export function Field({ label, name, id = name, defaultValue, type = 'text', required, disabled, placeholder, className, autoComplete }: {
  label: string, name: string, id?: string, defaultValue?: string | null, type?: string, required?: boolean, disabled?: boolean,
  placeholder?: string, className?: string, autoComplete?: string
}) {
  return (
    <div className={className}>
      <label htmlFor={id} className="block text-sm/6 font-medium text-gray-900">{label}</label>
      <input id={id} name={name} type={type} defaultValue={defaultValue ?? ''} required={required} disabled={disabled}
        placeholder={placeholder} autoComplete={autoComplete ?? 'off'} className={clsx('mt-1', inputClass)} />
    </div>
  )
}

export function RoleSelect({ label, name = 'role', defaultValue, roles, disabled }: {
  label: string, name?: string, defaultValue?: string | null, roles: { value: string, label: string }[], disabled?: boolean
}) {
  return (
    <div>
      <label htmlFor={name} className="block text-sm/6 font-medium text-gray-900">{label}</label>
      <select id={name} name={name} defaultValue={defaultValue ?? 'user'} disabled={disabled} className={clsx('mt-1', inputClass)}>
        {roles.map(r => <option key={r.value} value={r.value}>{r.label}</option>)}
      </select>
    </div>
  )
}

export function CompanySelect({ label, name = 'companyId', defaultValue, companies }: {
  label: string, name?: string, defaultValue?: string | null, companies: { id: string, name: string }[]
}) {
  return (
    <div>
      <label htmlFor={name} className="block text-sm/6 font-medium text-gray-900">{label}</label>
      <select id={name} name={name} defaultValue={defaultValue ?? undefined} className={clsx('mt-1', inputClass)}>
        {companies.map(c => <option key={c.id} value={c.id}>{c.name}</option>)}
      </select>
    </div>
  )
}

export function Alert({ kind, children }: { kind: 'error' | 'success' | 'info', children: React.ReactNode }) {
  const colors = {
    error: 'bg-red-50 text-red-800 ring-red-600/20',
    success: 'bg-green-50 text-green-800 ring-green-600/20',
    info: 'bg-blue-50 text-blue-800 ring-blue-600/20',
  }
  return <div className={clsx('rounded-md p-3 text-sm ring-1 ring-inset', colors[kind])}>{children}</div>
}

export function TemporaryPassword({ password, title, hint }: { password: string, title: string, hint: string }) {
  return (
    <div className="rounded-md bg-yellow-50 p-4 ring-1 ring-yellow-600/30">
      <div className="text-sm font-semibold text-yellow-900">{title}</div>
      <div className="mt-2 select-all font-mono text-lg tracking-wide text-gray-900">{password}</div>
      <p className="mt-2 text-sm text-yellow-900">{hint}</p>
    </div>
  )
}

export function SubmitButton({ children, pending, danger, secondary }: { children: React.ReactNode, pending?: boolean, danger?: boolean, secondary?: boolean }) {
  return (
    <button type="submit" disabled={pending}
      className={clsx('rounded-md px-3 py-2 text-sm font-semibold shadow-xs disabled:opacity-50',
        danger ? 'bg-red-600 text-white hover:bg-red-500'
          : secondary ? 'bg-white text-gray-900 ring-1 ring-gray-300 ring-inset hover:bg-gray-50'
            : 'bg-indigo-600 text-white hover:bg-indigo-500')}>
      {children}
    </button>
  )
}
