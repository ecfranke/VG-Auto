'use server'

import { httpPut  } from "@/_lib/server/query-api";
import {  pushToast } from "@/_lib/server/pushToast";
import { redirect } from "next/navigation"; 
import { IProduct } from "../model";
import { NIL as NIL_UUID } from 'uuid';

export async function createOrUpdateProducts(formData: FormData) {
       
    const workId = formData.get('workId');
   
    const activityId = formData.get('activityId');
    const activityNumber= formData.get('activityNumber');
    const activityName = formData.get('activityName');
    const redirectUrl = `/home/work/${workId}/${activityId}` ;
    const apiUrl = `work/${activityName}/${activityId}/productsorservices`;
    
    const ids = formData.getAll('id');
    const codes = formData.getAll('code');
    const names = formData.getAll('name');
    const prices = formData.getAll('price');
    const quantities = formData.getAll('quantity');
    const units = formData.getAll('unit');
    const discount = formData.getAll('discount');
    // empty or missing cells (e.g. a discount never typed) are sent as 0, never as NaN/null
    const num = (value: FormDataEntryValue | undefined, fallback = 0) => {
        const n = Number(value?.toString().replace(',', '.').trim() || NaN);
        return Number.isFinite(n) ? n : fallback;
    };
    const body = ids.map((id,index)=>{
        if(id.toString().startsWith('-')){
            //unsaved value
            id =NIL_UUID;
        }
        return {
            id:id,
            code:codes[index]?.toString().trim() ?? '',
            name:names[index],
            price:num(prices[index]),
            quantity:num(quantities[index], 1),
            unit:units[index],
            discount:num(discount[index]),
        } as IProduct
    })

     //isVehicelLinesOnPricing
    // const notes = formData.get('notes');
    //{id}/offer/{offerNumber}

    await httpPut({
        url:`work/${workId}/${activityName}/${activityNumber}`,
        body:{
            isVehicelLinesOnPricing:formData.get('isVehicelLinesOnPricing') == 'on',
            notes:formData.get('notes')
        }
    })

    const response =await httpPut({url:apiUrl,body});

    await response.text();
       
    pushToast(`Products and services updated successfully!`)

    redirect(redirectUrl)  
}
