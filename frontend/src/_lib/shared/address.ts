export interface IAddressParts { street?: string | null, city?: string | null, region?: string | null, postalCode?: string | null, country?: string | null }

/** Address lines as written in North America: street / city, province postal code / country. */
export function addressLines(a: IAddressParts | null | undefined): string[] {
  if (!a) return []
  const has = (x?: string | null) => !!x && x.trim() !== ''
  const regionLine = [a.region, a.postalCode].filter(has).map(x => x!.trim()).join(' ')
  const cityLine = [a.city, regionLine].filter(has).map(x => x!.trim()).join(', ')
  return [a.street, cityLine, a.country].filter(has).map(x => x!.trim())
}

/** "123 Main St, Vancouver, BC V6B 1A1, Canada" */
export function formatAddress(a: IAddressParts | null | undefined): string {
  return addressLines(a).join(', ')
}
