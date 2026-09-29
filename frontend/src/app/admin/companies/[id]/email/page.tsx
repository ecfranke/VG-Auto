import { notFound } from 'next/navigation'
import { httpRaw } from '@/_lib/server/query-api'
import { currentAccount } from '@/_lib/server/account'
import { getDictionary } from '../../../_i18n'
import { ICompanyEmail } from '../../../model'
import { Card } from '../../../_components/Page'
import { Alert } from '../../../_components/Fields'
import { AllowBuiltInSwitch, EmailSettingsForm, TestEmailForm } from '../../../_components/EmailSettingsForm'

/** How the company sends its estimates and invoices: its own SMTP, Microsoft 365 or Gmail account, or the built-in email when allowed. */
export default async function CompanyEmailPage({ params }: { params: Promise<{ id: string }> }) {
  const me = await currentAccount()
  if (!me.isAdmin) return null
  const { id } = await params
  if (!/^[0-9a-fA-F-]{36}$/.test(id)) notFound()
  const response = await httpRaw('GET', `admin/companies/${id}/email`)
  if (!response.ok) notFound()
  const email = await response.json() as ICompanyEmail
  const { t } = await getDictionary()

  return (
    <div className="space-y-6">
      {email.canAllowSystem && (
        <Card>
          <div className="flex items-start justify-between gap-x-6">
            <div>
              <h2 className="text-base font-semibold text-gray-900">{t.allowBuiltIn}</h2>
              <p className="mt-1 text-sm text-gray-500">{t.allowBuiltInHint}</p>
            </div>
            <AllowBuiltInSwitch companyId={id} allowed={email.systemAllowed} label={t.allowBuiltIn} />
          </div>
        </Card>
      )}

      <Card title={t.tabEmail} hint={t.companyEmailHint}>
        <div className="mb-6">
          {email.inUse
            ? <Alert kind="success">{t.inUse}: {email.inUse}</Alert>
            : <Alert kind="error">{t.cannotSend}</Alert>}
        </div>
        <EmailSettingsForm key={id} t={t} mode="company"
          settings={email.settings} companyId={id} systemAllowed={email.systemAllowed} />
      </Card>

      <Card title={t.testEmail} hint={t.testEmailHint}>
        <TestEmailForm t={t} companyId={id} defaultTo={me.email ?? ''} />
      </Card>
    </div>
  )
}
