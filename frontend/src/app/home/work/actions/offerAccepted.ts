'use server';
import { pushToast } from "@/_lib/server/pushToast";
import {  httpPut } from "@/_lib/server/query-api";
import { redirect } from "next/navigation";


export async function offerAccepted({
    workId,
    offerNumber, 
    targetJobNumber,
    notes,
}:{
    workId: string, 
    offerNumber: number, 
    targetJobNumber:string,
    notes:string 
}) {
 
    const response = await httpPut({
        url: `work/${workId}/estimate/${offerNumber}/accepted/${targetJobNumber}`,
        body: notes
    });
    const repairJobId= await response.json();
    
    if(targetJobNumber){
        pushToast(`Offer changed into accepted state and repair job updated.`)
    }
    else pushToast(`Offer changed into accepted state and new repair job created successfully.`)
 
    redirect(`/home/work/${workId}/${repairJobId}/edit`);
} 