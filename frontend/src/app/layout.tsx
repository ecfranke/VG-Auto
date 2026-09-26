import { type Metadata } from 'next'
import { Inter, Lexend } from 'next/font/google'
import clsx from 'clsx'
import { config } from '@fortawesome/fontawesome-svg-core'
import '@fortawesome/fontawesome-svg-core/styles.css'
config.autoAddCss = false

import '@/_styles/tailwind.css'
  
export const metadata: Metadata = {
  title: {
    template: '%s', // - B-dec
    default: 'Repair and maintenance',
  },
  description:
    'Streamline your car repair business with our all-in-one web app. Track work progress, create jobs, manage services and products, generate offers, issue invoices, and organize your cars and clients effortlessly. Designed to help you save time, boost productivity, and grow your business—your workshop deserves more than just tools; it deserves a partner in success.',
}

const inter = Inter({
  subsets: ['latin'],
  display: 'swap',
  variable: '--font-inter',
})

const lexend = Lexend({
  subsets: ['latin'],
  display: 'swap',
  variable: '--font-lexend',
})

export default function DefaultLayout({
  children,
}: {
  children: React.ReactNode
}) {
 
  return (
    <html className={clsx(
      'h-full xl:bg-gray-50  ',
      inter.variable,
      lexend.variable,
    )}>
      <head>
        <link rel="icon" href="/favicon.ico" sizes="any" />
      </head>
      <body className=" h-full ">
     
        {children}
      </body>
    </html>
  )
}

 