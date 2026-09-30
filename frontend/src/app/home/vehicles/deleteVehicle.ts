'use server'

import { httpDelete } from '@/_lib/server/query-api'
import { pushToast } from '@/_lib/server/pushToast'
import { redirect } from 'next/navigation'

/** Deletes a vehicle no work order uses (administrators only); otherwise the API explains why (shown as a toast). */
export async function deleteVehicle(id: string) {
  const response = await httpDelete({ url: 'vehicles', body: [id] })
  await response.text()
  pushToast('Vehicle deleted.')
  redirect('/home/vehicles')
}
