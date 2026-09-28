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
}

export interface ICompany {
  id: string
  name: string
  regNo: string | null
  currency: string
  employees: number
  users: number
}
