import 'server-only'
import { cookies } from 'next/headers'
import type { Theme } from '@/_components/ThemeSwitch'

/** The theme chosen with ThemeSwitch; "system" follows the device. */
export async function currentTheme(): Promise<Theme> {
  const value = (await cookies()).get('theme')?.value
  return value === 'light' || value === 'dark' ? value : 'system'
}
