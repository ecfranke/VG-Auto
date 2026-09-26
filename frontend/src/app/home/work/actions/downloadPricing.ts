'use server'; 
import { httpGet } from "@/_lib/server/query-api"; 


export async function downloadPricing({
    pricingId,
    pricingName,
    downloadHtml, 
}:{
    pricingId:string,
    pricingName:string,
    downloadHtml?:boolean| undefined 
}) {

 
    const response = await httpGet(`pricings/${pricingName}/${pricingId}/${downloadHtml?'html':'pdf'}`);

    const responseObj = downloadHtml? (await response.text()):(await response.blob());
 
    return responseObj;
}
