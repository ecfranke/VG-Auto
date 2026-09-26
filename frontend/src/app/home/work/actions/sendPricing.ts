'use server';
import { pushToast } from "@/_lib/server/pushToast";
import { httpPut } from "@/_lib/server/query-api";
import { redirect } from "next/navigation";

export async function sendPricing({
    workId, 
    offerId, 
    clientEmail,
}:{
    workId:string, 
    clientEmail:string, 
    offerId?:string, 
}) {

    if(offerId){
        const response = await httpPut({
            url: `work/estimate/send/${offerId}`,
            body: {
                emailAddress:clientEmail 
            }
        });
        await response.text();
        pushToast('Offer sent.')
        redirect(`/home/work/${workId}/${offerId}`); 
    }

    const response = await httpPut({
        url: `work/${workId}/invoice/send`,
        body: {
            emailAddress:clientEmail 
        }
    });
    await response.text();
    pushToast('Invoice sent.')
    redirect(`/home/work/${workId}`);
   
}
