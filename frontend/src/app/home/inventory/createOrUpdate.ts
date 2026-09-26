'use server'

import { httpPut,httpPost } from "@/_lib/server/query-api";
import {  pushToast } from "@/_lib/server/pushToast";
import { redirect } from "next/navigation"; 


export async function createOrUpdate(
    formData: FormData
    ) {
       
    const id =   formData.get('id') ;
    
    let storageId = formData.get('storage'); 
    if(!storageId) storageId = null;
    let price = formData.get('price')?.toString();
    if(!price) price = '0'; 
    let quantity = formData.get('quantity')?.toString();
    if(!quantity) quantity = '0'; 
  
    const body = {
        code: formData.get('code'),
        name: formData.get('name'),
        price: price,
        quantity: quantity,
        storageId: storageId,
        description: formData.get('about'), 
    };
 
    const url = "spareparts";
     
    const isUpdating = !!id;
    const response = isUpdating?await httpPut({url:url+'/'+id,body}) : await httpPost({url,body});

    const jsonResponse = await response.json();
      
    const responseId = jsonResponse ; 
   
    pushToast(`Spare part ${(isUpdating?'updated':'saved')} successfully!`)

    redirect('/home/inventory/' +responseId) 
}
