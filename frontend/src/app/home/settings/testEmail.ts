'use server'

import { httpPost } from "@/_lib/server/query-api";
import { pushToast } from "@/_lib/server/pushToast";
import { redirect } from "next/navigation";

export async function sendTestEmail(formData: FormData) {
  const to = formData.get('testEmailTo')?.toString() ?? '';
  const response = await httpPost({ url: 'options/testemail', body: { to } });
  const result = await response.json();
  pushToast(`Test email sent to ${to} via ${result.transport}.`);
  redirect('/home/settings');
}
