'use client'
 
import Link from 'next/link'
import { useSearchParams } from 'next/navigation'
import { Suspense } from 'react'

function Error(){
  const searchParams = useSearchParams() 
  const code = searchParams.get('code')
  const text = searchParams.get('text')
  const statusText = searchParams.get('statusText');
  return (
    <> 
      <main className="grid min-h-full place-items-center bg-white px-6 py-24 sm:py-32 lg:px-8">
        <div className="text-center">
          <p className="text-base font-semibold text-indigo-600">{code} {statusText}</p>
          <h1 className="mt-4 text-3xl font-semibold tracking-tight text-balance text-gray-900 sm:text-5xl">
            Oops, something went wrong
          </h1>
          <p className="mt-6 text-lg font-medium text-pretty text-gray-500 sm:text-xl/8">
            {text}
          </p>
          <div className="mt-10 flex items-center justify-center gap-x-6">
            <Link
              href="/home/work"
              className="rounded-md bg-indigo-600 px-3.5 py-2.5 text-sm font-semibold text-white shadow-xs hover:bg-indigo-500 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-indigo-600"
            >
              Go back home
            </Link>
            <a href="/support" className="text-sm font-semibold text-gray-900">
              Contact support <span aria-hidden="true">&rarr;</span>
            </a>
          </div>
        </div>
      </main>
    </>
  )
}

export default function ErrorWrappedInSuspense() { //tslint complains
      return (
        <Suspense>
          <Error></Error>
        </Suspense>
      )
  }
  