import { redirect } from 'next/navigation'

/** Old address of the company settings. */
export default function CompanyPage() {
  redirect('/admin/companies')
}
