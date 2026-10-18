'use server'

import { httpPut,httpPost } from "@/_lib/server/query-api";
import {  pushToast } from "@/_lib/server/pushToast";
import { redirect } from "next/navigation"; 

export async function createOrUpdate(
    formData: FormData
    ) {
      
     
    const id = formData.get('id');
    // the vehicle comes from the search box (vehicleId[value]) or from the list of the client's vehicles (vehicleId);
    // without a client only the search box is shown, so look at both (the vehicle was lost before)
    const pickedVehicle = (formData.get('vehicleId[value]') ?? formData.get('vehicleId'))?.toString() ?? ''
    const vehicleId = pickedVehicle === '' ? null : pickedVehicle;
    const clientId = formData.get('clientId[value]');
 //Guid? ClientId, string Description, Guid? VehicleId, Guid[] AssignedTo, int? Odo, bool StartWithOffer
    const body = {
       clientId:clientId ==''?null:clientId,
       description: formData.get('about'),
       vehicleId,
       assignedTo:formData.getAll('mechanics'),
       odo: +(formData.get('odo')?.toString()??'0'),
       startWithOffer: formData.get('isOffer') == 'on'
    }; 
    const url = "work"; 
    
    const isUpdating = !!id;
    
    const response = isUpdating?await httpPut({url:url+'/'+id,body}) : await httpPost({url,body});

    const jsonResponse = await response.json();
       
    pushToast(`Work ${(isUpdating?'updated':'saved')} successfully!`)

    if(id)
        redirect(`/home/work/${jsonResponse}`) 

    if(isUpdating){
        redirect(`/home/work/${jsonResponse.workId}/${jsonResponse.activityId}`) 
    }
    redirect(`/home/work/${jsonResponse.workId}/${jsonResponse.activityId}/edit/startfresh`) 
    
}

