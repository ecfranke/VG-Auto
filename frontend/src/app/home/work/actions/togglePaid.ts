'use server';
import { pushToast } from "@/_lib/server/pushToast";
import { httpPut } from "@/_lib/server/query-api";
import { redirect } from "next/navigation";


export async function togglePaid(workId: string,isPaid:boolean) {

    const response = await httpPut({
        url: `work/${workId}/invoice/paid`,
        body:isPaid
    });
    await response.text();
    pushToast('Payment status changed successfully.')

    redirect(`/home/work/${workId}`);
} 