'use server'

import { httpDelete } from '@/_lib/server/query-api'
import { pushToast } from '@/_lib/server/pushToast'
import { redirect } from 'next/navigation'

/** Deletes a client that nothing refers to; otherwise the API explains why (shown as a toast). */
export async function deleteClient(id: string) {
  const response = await httpDelete({ url: 'clients', body: [id] })
  await response.text()
  pushToast('Client deleted.')
  redirect('/home/clients')
}
