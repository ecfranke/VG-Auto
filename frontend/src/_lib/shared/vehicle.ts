/** "2019 Honda Civic LX" */
export function vehicleTitle(v: { year?: number | string | null, manufacturer?: string | null, model?: string | null, trim?: string | null } | null | undefined): string {
  if (!v) return ''
  return [v.year, v.manufacturer, v.model, v.trim].filter(x => x !== null && x !== undefined && String(x).trim() !== '').join(' ')
}

/** "2019 Honda Civic (ABC 123)" */
export function vehicleLabel(v: { year?: number | string | null, manufacturer?: string | null, model?: string | null, licensePlate?: string | null } | null | undefined): string {
  if (!v) return ''
  const title = vehicleTitle(v)
  return v.licensePlate ? `${title}${title ? ' ' : ''}(${v.licensePlate})` : title
}
