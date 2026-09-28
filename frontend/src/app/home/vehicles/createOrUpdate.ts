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

    let ownerId = formData.get('ownerId[value]');
    if(!ownerId) ownerId = null;

    const yearText = (formData.get('year')?.toString() ?? '').trim();
    const year = yearText ? Number(yearText) : null;
    if (year !== null && (!Number.isInteger(year) || year < 1900 || year > new Date().getFullYear() + 1)) {
        await pushToast(`Year must be between 1900 and ${new Date().getFullYear() + 1}.`, true);
        redirect(id ? `/home/vehicles/edit/${id}` : '/home/vehicles/new');
    }

    const body = {
        model: formData.get('model'),
        year,
        manufacturer: formData.get('manufacturer[name]'),
        vin: formData.get('vin'),
        licensePlate: formData.get('licensePlate'),
        odo: odo,
        description: formData.get('about'),
        ownerId:ownerId
    };
 
    const url = "vehicles";
     
    const isUpdating = !!id;
    const response = isUpdating?await httpPut({url:url+'/'+id,body}) : await httpPost({url,body});

    const jsonResponse = await response.json();
      
    const vehicleId =  jsonResponse ; 
   
    pushToast(`Vehicle ${(isUpdating?'updated':'saved')} successfully!`)

    redirect('/home/vehicles/' +vehicleId) 
}