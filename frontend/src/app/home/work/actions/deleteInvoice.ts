'use server';
import { pushToast } from "@/_lib/server/pushToast";
import { httpPut } from "@/_lib/server/query-api";
import { redirect } from "next/navigation";


export async function deleteInvoice(workId: string) {

    const response = await httpPut({
        url: `work/${workId}/invoice/delete`,
        body: {}
    });
    await response.text();
    pushToast('Invoice deleted successfully.')
    redirect(`/home/work/${workId}`);
}
