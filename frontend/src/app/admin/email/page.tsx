import Link from 'next/link'
import { redirect } from 'next/navigation'
import { httpGet } from '@/_lib/server/query-api'
import { currentAccount } from '@/_lib/server/account'
import { getDictionary } from '../_i18n'
import { ISystemEmail } from '../model'
import { Card, PageHeader, Table } from '../_components/Page'
import { Alert } from '../_components/Fields'
import { Badge } from '../_components/Badges'
import { AllowBuiltInSwitch, EmailSettingsForm, TestEmailForm } from '../_components/EmailSettingsForm'

/** The built-in email of the system and the companies that may use it (super administrators). */
export default async function SystemEmailPage() {
  const me = await currentAccount()
  if (!me.isAdmin) return null
  if (!me.isSuperAdmin) redirect(`/admin/companies/${me.companyId}/email`)
  const { t } = await getDictionary()
  const email = await (await httpGet('admin/email/system')).json() as ISystemEmail

  return (
    <div className="space-y-6">
      <PageHeader title={t.builtInEmail} hint={t.builtInEmailHint} />

      <Card>
        <Alert kind="success">{t.inUse}: {email.inUse}</Alert>
        {!email.editable && <div className="mt-4"><Alert kind="info">{t.notEditable}</Alert></div>}
        <div className="mt-6">
          <EmailSettingsForm t={t} mode="system" settings={email.settings} editable={email.editable} />
        </div>
        <p className="mt-4 text-xs text-gray-500">{t.serverConfigurationNow} {email.serverConfiguration}</p>
      </Card>

      <Card title={t.testEmail} hint={t.testEmailHint}>
        <TestEmailForm t={t} defaultTo={me.email ?? ''} />
      </Card>

      <div className="space-y-3">
        <div>
          <h2 className="text-base font-semibold text-gray-900">{t.companiesBuiltIn}</h2>
          <p className="mt-1 text-sm text-gray-500">{t.companiesBuiltInHint}</p>
        </div>
        <Table head={[t.companyName, t.inUse, { label: t.allowBuiltIn, right: true }]} empty={email.companies.length === 0} emptyText={t.empty}>
          {email.companies.map(c => (
            <tr key={c.id} className="hover:bg-gray-50">
              <td className="px-4 py-3">
                <Link href={`/admin/companies/${c.id}/email`} className="font-medium text-link hover:text-link-hover">{c.name}</Link>
              </td>
              <td className="px-4 py-3">
                {c.kind === 'smtp' || c.kind === 'graph' || c.kind === 'gmail'
                  ? <Badge color="blue">{c.description}</Badge>
                  : c.systemAllowed ? <Badge color="gray">{t.builtInEmail}</Badge> : <Badge color="yellow">{t.noEmailCompanies}</Badge>}
              </td>
              <td className="px-4 py-3 text-right">
                <AllowBuiltInSwitch key={`${c.id}-${c.systemAllowed}`} companyId={c.id} allowed={c.systemAllowed} label={`${t.allowBuiltIn}: ${c.name}`} />
              </td>
            </tr>
          ))}
        </Table>
      </div>
    </div>
  )
}
