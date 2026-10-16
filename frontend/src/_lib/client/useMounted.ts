import { useSyncExternalStore } from 'react'

const subscribe = () => () => {}

/**
 * False while the page is rendered on the server and hydrated, true afterwards. Headless UI dialogs are only
 * rendered once this is true: a closed dialog renders a hidden helper element that the client does not always
 * render the same way, which made React report a hydration error ("Minified React error #418") in the console.
 */
export function useMounted(): boolean {
  return useSyncExternalStore(subscribe, () => true, () => false)
}
