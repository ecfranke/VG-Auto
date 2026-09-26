'use server';
import { pushToast } from "@/_lib/server/pushToast";
import {   httpPut } from "@/_lib/server/query-api";
import { redirect } from "next/navigation";


export async function issueAnOffer({
    workId,
    activityId,
    offerNumber,
    showVehicleOnPricing,
    sendClientEmail,
    clientEmail 
}:{
    workId: string,
    activityId:string,
    offerNumber: number,
    showVehicleOnPricing: boolean,
    sendClientEmail: boolean,
    clientEmail:string 
}) {
 
    const response = await httpPut({
        url: `work/${workId}/estimate/issue/${offerNumber}`,
        body: {
            showVehicleOnPricing,
            sendClientEmail,
            clientEmail
        }
    });
    const workingOfferId = await response.json();
    
    const issuingType = workingOfferId == activityId ? 'issued' : 're-issued';
    if(sendClientEmail){
        pushToast(`Offer ${issuingType} successully and email sent to client.`)
    }
    else pushToast(`Offer ${issuingType} successully.`)
 
    redirect(`/home/work/${workId}/${workingOfferId}`);
} 