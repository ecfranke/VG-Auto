import Image from 'next/image';

/** Two column layout used by the sign in pages. */
export default function AuthShell({ title, subtitle, children }: { title: string, subtitle?: React.ReactNode, children: React.ReactNode }) {
  return (
    <div className="bg-white flex min-h-full flex-1">
      <div className="flex flex-1 flex-col justify-center px-4 py-12 sm:px-6 lg:flex-none lg:px-20 xl:px-24">
        <div className="mx-auto w-full max-w-sm lg:w-96">
          <div>
            <Image alt="Logo" width="50" height="50" className="h-10 w-auto" src="/logo.png" />
            <h2 className="mt-8 text-2xl/9 font-bold tracking-tight text-gray-900">{title}</h2>
            {subtitle && <p className="mt-2 text-sm/6 text-gray-500">{subtitle}</p>}
          </div>
          <div className="mt-10">{children}</div>
        </div>
      </div>
      <div className="relative hidden w-0 flex-1 lg:block">
        <Image alt="" src="/m2.webp" width="1840" height="1380" className="absolute inset-0 size-full object-cover" />
      </div>
    </div>
  );
}

export const inputClass = "block w-full rounded-md bg-white px-3 py-1.5 text-base text-gray-900 outline-1 -outline-offset-1 outline-gray-300 placeholder:text-gray-400 focus:outline-2 focus:-outline-offset-2 focus:outline-indigo-600 text-sm/6";
export const primaryButtonClass = "flex w-full justify-center rounded-md bg-indigo-600 px-3 py-1.5 text-sm/6 font-semibold text-white shadow-xs hover:bg-indigo-500 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-indigo-600 disabled:opacity-50";
export const linkClass = "font-semibold text-indigo-600 hover:text-indigo-500";
