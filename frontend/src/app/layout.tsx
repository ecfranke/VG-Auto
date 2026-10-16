import { type Metadata } from 'next'
import { Inter } from 'next/font/google'
import clsx from 'clsx'
import { config } from '@fortawesome/fontawesome-svg-core'
import '@fortawesome/fontawesome-svg-core/styles.css'
config.autoAddCss = false

import '@/_styles/tailwind.css'
import { currentTheme } from '@/_lib/server/theme'
  
export const metadata: Metadata = {
  title: {
    template: '%s | VG Auto',
    default: 'VG Auto',
  },
  applicationName: 'VG Auto',
  description:
    'VG Auto: workshop management for repair shops. Work orders, estimates, invoices, clients, vehicles and inventory.',
}

const inter = Inter({
  subsets: ['latin'],
  display: 'swap',
  variable: '--font-inter',
})


/**
 * When a script or style file of the app cannot be loaded (ChunkLoadError, 404 after an update while an old page
 * was open or cached), the page is loaded once more with a query that bypasses caches; never in a loop.
 */
const CHUNK_RECOVERY = `(function(){var k='vg-chunk-reload';
try{var u=new URL(location.href);if(u.searchParams.has('_r')){u.searchParams.delete('_r');history.replaceState(history.state,'',u.pathname+u.search+u.hash)}}catch(_){}
function retry(){try{if(sessionStorage.getItem(k))return;sessionStorage.setItem(k,'1')}catch(_){return}
var u=new URL(location.href);u.searchParams.set('_r',Date.now());location.replace(u.toString())}
addEventListener('error',function(e){var t=e&&e.target,m=(e&&e.message)||'';
if(/ChunkLoadError|Loading (CSS )?chunk/.test(m)||(t&&(t.tagName==='SCRIPT'||t.tagName==='LINK')&&(t.src||t.href||'').indexOf('/_next/static/')>=0))retry()},true);
addEventListener('unhandledrejection',function(e){var r=e&&e.reason;if(r&&(r.name==='ChunkLoadError'||/Loading (CSS )?chunk/.test(r.message||'')))retry()});
addEventListener('load',function(){setTimeout(function(){try{sessionStorage.removeItem(k)}catch(_){}},15000)})})();`

export default async function DefaultLayout({
  children,
}: {
  children: React.ReactNode
}) {
  // light / dark as chosen by the user; without a choice the CSS follows the system
  const theme = await currentTheme()
  return (
    <html lang="en" className={clsx(
      'h-full bg-surface xl:bg-gray-50',
      theme !== 'system' && theme,
      inter.variable,
    )}>
      <head>
        <link rel="icon" href="/favicon.ico" sizes="any" />
        <link rel="icon" href="/logo.svg" type="image/svg+xml" />
        <link rel="apple-touch-icon" href="/apple-touch-icon.png" />
        {/* a page from before an update can ask for script/style files the new version no longer has: load the page again once */}
        <script dangerouslySetInnerHTML={{ __html: CHUNK_RECOVERY }} />
      </head>
      <body className=" h-full ">
     
        {children}
      </body>
    </html>
  )
}

 