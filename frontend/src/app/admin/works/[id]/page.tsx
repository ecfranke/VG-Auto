import { notFound, redirect } from 'next/navigation'
import { httpRaw } from '@/_lib/server/query-api'
import { currentAccount } from '@/_lib/server/account'
import { getDictionary } from '../../_i18n'
import { IAdminWork } from '../../model'
import { Card, PageHeader, Table } from '../../_components/Page'
import { Badge } from '../../_components/Badges'
import DeleteWorkButton from './DeleteWorkButton'

/** A work of any company with its documents (super administrators). */
export default async function WorkPage({ params }: { params: Promise<{ id: string }> }) {
  const me = await currentAccount()
  if (!me.isAdmin) return null
  if (!me.isSuperAdmin) redirect('/admin')
  const { id } = await params
  if (!/^[0-9a-fA-F-]{36}$/.test(id)) notFound()
  const response = await httpRaw('GET', `admin/works/${id}`)
  if (!response.ok) notFound()
  const work = await response.json() as IAdminWork
  const { t, lang } = await getDictionary()
  const date = new Intl.DateTimeFormat(lang === 'zh' ? 'zh-CN' : 'en-GB', { dateStyle: 'medium' })
  const dateTime = new Intl.DateTimeFormat(lang === 'zh' ? 'zh-CN' : 'en-GB', { dateStyle: 'medium', timeStyle: 'short' })
  const hasInvoice = work.documents.some(d => d.kind === 'invoice')

  const item = (label: string, value: React.ReactNode) => (
    <div>
      <dt className="text-sm text-gray-500">{label}</dt>
      <dd className="mt-1 text-sm font-medium text-gray-900">{value || '—'}</dd>
    </div>
  )

  return (
    <div className="space-y-6">
      <PageHeader title={<span className="font-mono text-lg break-all">{work.code}</span>} back={{ href: '/admin/works', label: t.works }}>
        <Badge color={work.status === 'completed' ? 'purple' : 'gray'}>{t.workStatus[work.status] ?? work.status}</Badge>
      </PageHeader>

      <Card>
        <dl className="grid grid-cols-1 gap-5 sm:grid-cols-3">
          {item(t.companyOf, work.companyName)}
          {item(t.client, work.clientName && <>{work.clientName}{work.clientEmail && <div className="font-normal text-gray-500">{work.clientEmail}</div>}</>)}
          {item(t.vehicle, work.vehicle && <>{work.vehicle}{work.licensePlate && <div className="font-normal text-gray-500">{work.licensePlate}</div>}</>)}
          {item(t.started, date.format(new Date(work.startedOn)))}
          {item(t.startedBy, work.startedBy)}
          {item(t.repairJobs, String(work.repairJobs))}
          {work.notes && <div className="sm:col-span-3">{item(t.description, <span className="font-normal whitespace-pre-line">{work.notes}</span>)}</div>}
        </dl>
      </Card>

      <div className="space-y-3">
        <h2 className="text-base font-semibold text-gray-900">{t.documents}</h2>
        <Table head={[t.document, t.issued, t.sent, t.status, { label: t.amount, right: true }, '']} empty={work.documents.length === 0} emptyText={t.noDocuments}>
          {work.documents.map(d => (
            <tr key={d.id}>
              <td className="px-4 py-3">
                <div className="text-xs text-gray-500">{t.kinds[d.kind]}</div>
                <div className="font-mono text-xs font-semibold break-all text-gray-900">{d.code}</div>
              </td>
              <td className="px-4 py-3 text-gray-700">
                {dateTime.format(new Date(d.issuedOn))}
                {d.issuedBy && <div className="text-xs text-gray-500">{d.issuedBy}</div>}
              </td>
              <td className="px-4 py-3 text-gray-700">
                {d.sentOn ? <>{dateTime.format(new Date(d.sentOn))}<div className="text-xs break-all text-gray-500">{d.sentTo}</div></> : <span className="text-gray-400">{t.notSent}</span>}
              </td>
              <td className="px-4 py-3">
                <span className="flex flex-wrap gap-1">
                  {d.acceptedOn && <Badge color="green">{t.accepted}</Badge>}
                  {d.signedBy && <span title={d.signedOn ? dateTime.format(new Date(d.signedOn)) : undefined}><Badge color="green">{t.signedByLabel} {d.signedBy}</Badge></span>}
                  {d.paid === true && <Badge color="green">{t.paid}</Badge>}
                  {d.paid === false && <Badge color="blue">{t.unpaid}</Badge>}
                </span>
              </td>
              <td className="px-4 py-3 text-right whitespace-nowrap text-gray-900">{d.total}</td>
              <td className="px-4 py-3 text-right">
                <a href={`/admin/works/${work.id}/pdf/${d.id}`} target="_blank" className="text-sm font-semibold text-link hover:text-link-hover">PDF</a>
              </td>
            </tr>
          ))}
        </Table>
      </div>

      <Card title={t.deleteWork} hint={t.deleteWorkHint} className="ring-red-600/30">
        {hasInvoice && <p className="-mt-2 mb-4 text-sm text-red-700">{t.invoiceGapHint}</p>}
        <DeleteWorkButton workId={work.id} code={work.code} label={t.deleteWork} confirmText={t.confirmDeleteWork} />
      </Card>
    </div>
  )
}
