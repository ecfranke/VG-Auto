'use server'

import { httpPut,httpPost } from "@/_lib/server/query-api";
import {  pushToast } from "@/_lib/server/pushToast";
import { redirect } from "next/navigation"; 

export async function createOrUpdate(
    formData: FormData
    ) {
      
    const id = formData.get('id') ;

    let odo = formData.get('odo');
    if(!odo) odo = '0';

    debugger;
    let ownerId = formData.get('ownerId[value]');
    if(!ownerId) ownerId = null;

    const body = {
        model: formData.get('model'),
        producer: formData.get('producer[name]'),
        vin: formData.get('vin'),
        regNr: formData.get('regNr'),
        odo: odo,
        description: formData.get('about'),
        ownerId:ownerId
    };
 
    debugger;
    const url = "vehicles";
     
    const isUpdating = !!id;
    const response = isUpdating?await httpPut({url:url+'/'+id,body}) : await httpPost({url,body});

    const jsonResponse = await response.json();
      
    const vehicleId =  jsonResponse ; 
   
    pushToast(`Vehicle ${(isUpdating?'updated':'saved')} successfully!`)

    redirect('/home/vehicles/' +vehicleId) 
}