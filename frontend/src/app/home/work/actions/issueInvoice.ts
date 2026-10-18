'use server';
import { pushToast } from "@/_lib/server/pushToast";
import { httpPut } from "@/_lib/server/query-api";
import { redirect } from "next/navigation";


export async function issueInvoice({
    workId,
    dueDays,
    paymentType,
    sendClientEmail,
    clientEmail,
    showVehicleOnPricing,
}:{
    workId:string,
    dueDays:number,
    paymentType:number,
    sendClientEmail:boolean,
    clientEmail:string, 
    showVehicleOnPricing?:boolean,
}) {

    const response = await httpPut({
        url: `work/${workId}/invoice/issue`,
        body: {
            dueDays,
            paymentType,
            sendClientEmail,
            clientEmail,
            showVehicleOnPricing,
        }
    });
    await response.text();
    pushToast('Work completed and invoice issued successfully.')
    redirect(`/home/work/${workId}`);
}
