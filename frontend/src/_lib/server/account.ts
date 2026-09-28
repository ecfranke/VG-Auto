import 'server-only'
import { httpGet } from './query-api';

export interface IAccount {
  userName: string;
  fullName: string;
  role: 'user' | 'admin' | 'superadmin';
  isOwner: boolean;
  isAdmin: boolean;
  companyId: string;
  email: string | null;
}

/** The signed in account with its current role (read from the API, not from the token). */
export async function currentAccount(): Promise<IAccount> {
  const response = await httpGet('admin/me');
  return await response.json() as IAccount;
}
