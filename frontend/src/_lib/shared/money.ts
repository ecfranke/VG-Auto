/** Currency formatting shared by server and client components. Mirrors VgAuto.Core.Domain.Currencies. */
export const DEFAULT_CURRENCY = 'CAD'

const CULTURES: Record<string, string> = {
  CAD: 'en-CA', USD: 'en-US', EUR: 'de-DE', GBP: 'en-GB', CNY: 'zh-CN', HKD: 'zh-HK', TWD: 'zh-TW',
  JPY: 'ja-JP', KRW: 'ko-KR', AUD: 'en-AU', NZD: 'en-NZ', SGD: 'en-SG', CHF: 'de-CH', MXN: 'es-MX',
}

export function normalizeCurrency(code?: string | null): string {
  const value = (code ?? '').trim().toUpperCase()
  return CULTURES[value] ? value : DEFAULT_CURRENCY
}

/** "$1,234.50" for CAD, "1.234,50 €" for EUR, "￥1,235" for JPY. */
export function formatMoney(amount: number | null | undefined, currency?: string | null): string {
  if (amount === null || amount === undefined || Number.isNaN(amount)) return ''
  const code = normalizeCurrency(currency)
  return new Intl.NumberFormat(CULTURES[code], { style: 'currency', currency: code }).format(amount)
}
