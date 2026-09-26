'use server'

import { httpPost, httpDelete } from "@/_lib/server/query-api";
import {  pushToast } from "@/_lib/server/pushToast"; 


export async function removeLocation(
    locationId : string,
    ) {
        
      await httpDelete({
        url:"storages",
        body:[locationId]
      })
      pushToast(`Location removed.`) 
}


export async function addLocation(
  newName : string,
  ) {
     
    const locationResponse = await httpPost({
      url:"storages",
      body:{
          name:newName
      }
    })

    const newLocationId = await locationResponse.json();

    pushToast(`New location added successfully`) 

    return newLocationId;
}
