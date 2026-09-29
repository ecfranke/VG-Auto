import { redirect } from 'next/navigation'
import { currentAccount } from '@/_lib/server/account'

/** The company of the signed in administrator. */
export default async function MyCompanyPage() {
  const me = await currentAccount()
  redirect(me.isAdmin ? `/admin/companies/${me.companyId}` : '/admin')
}
