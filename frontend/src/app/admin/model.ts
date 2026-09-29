export type Role = 'user' | 'admin' | 'superadmin'

export interface IAdminUser {
  employeeId: string
  firstName: string
  lastName: string
  email: string | null
  phone: string | null
  profession: string | null
  description: string | null
  hasAccount: boolean
  userName: string | null
  role: Role | null
  isOwner: boolean
  disabled: boolean
  locked: boolean
  mustChangePassword: boolean
  microsoftLinked: boolean
  isSelf: boolean
  allowedActions: string[]
  companyId: string
  companyName: string | null
}

export interface IAuditEntry {
  id: number
  createdAt: string
  actor: string
  action: string
  target: string | null
  details: string | null
  /** the company the entry is about; null for the whole system */
  companyId: string | null
}

export interface ICompany {
  id: string
  name: string
  regNo: string | null
  currency: string
  employees: number
  users: number
}

/** system: the built-in email (companies) | config: the server configuration (built-in email) | smtp | graph (Microsoft 365) | gmail */
export type EmailKind = 'system' | 'config' | 'smtp' | 'graph' | 'gmail'

export interface IEmailSettings {
  kind: EmailKind
  fromAddress: string | null
  fromName: string | null
  smtpHost: string | null
  smtpPort: number | null
  smtpUser: string | null
  hasSmtpPassword: boolean
  smtpSecurity: 'Auto' | 'None' | 'StartTls' | 'SslOnConnect'
  graphTenantId: string | null
  graphClientId: string | null
  hasGraphClientSecret: boolean
  graphSender: string | null
  updatedAt: string | null
  /** the saved password or secret can no longer be decrypted (the server key changed) */
  unreadableSecret: boolean
  description: string | null
}

export interface ICompanyEmailRow {
  id: string
  name: string
  kind: EmailKind
  systemAllowed: boolean
  description: string
}

export interface ISystemEmail {
  settings: IEmailSettings
  inUse: string
  serverConfiguration: string
  /** false when several tenants share the server: the built-in email then comes from the server configuration */
  editable: boolean
  companies: ICompanyEmailRow[]
}

export interface ICompanyEmail {
  companyId: string
  companyName: string
  settings: IEmailSettings
  systemAllowed: boolean
  /** the signed in account may allow the built-in email (super administrators) */
  canAllowSystem: boolean
  /** null: the company cannot send email */
  inUse: string | null
}

export interface IOverview {
  companies: number
  employees: number
  logins: number
  administrators: number
  disabledLogins: number
  systemEmail: string
  systemEmailSaved: boolean
  builtInEmailCompanies: number
  ownEmailCompanies: number
  noEmailCompanies: number
  recent: IAuditEntry[]
}
