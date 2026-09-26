'use server';
import { pushToast } from "@/_lib/server/pushToast";
import { httpPut } from "@/_lib/server/query-api";
import { redirect } from "next/navigation";


export async function changeWorkStatus(workId: string,status:string) {

    const response = await httpPut({
        url: `work/${workId}/status/${status}`,
        body:{}
    });
    await response.text();
    pushToast('Status changed successfully.')

    redirect(`/home/work/${workId}`);
} 