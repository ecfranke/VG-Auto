'use server'

import { httpPut,httpPost } from "@/_lib/server/query-api";
import {  pushToast } from "@/_lib/server/pushToast";
import { redirect } from "next/navigation";

export async function createOrUpdate(
    formData: FormData
    ) {
     
    const emailAddresses = formData.getAll('emails').filter(x=>x);
    
    const isCompany = formData.get('isCompany') === 'on';
     
    const id =  formData.get('id');
    
    const body = {
        name: formData.get('name'),
        regNr: formData.get('regNr'),
        firstName: formData.get('first-name'),
        lastName: formData.get('last-name'),
        emailAddresses: emailAddresses,
        currentEmail: formData.get('currentEmail'),
        phone: formData.get('phone'),

        address: {
            street: formData.get('street-address[street]'),
            country: formData.get('country'),
            region: formData.get('region'),
            city: formData.get('city'),
            postalCode: formData.get('postal-code'),
        },

        description: formData.get('about'),
        isAsshole: formData.get('complicated') == 'on',
        personalCode: formData.get('personal-code'),
        introducedAt: new Date(),
    };
    const url = isCompany ? "legalclients" : "privateclients";
   
    const isUpdating = !!id;
    const response = isUpdating?await httpPut({url:url+'/'+id,body}) : await httpPost({url,body});

    const jsonResponse = await response.json();
      
    const clientId =  jsonResponse ; 
   
    pushToast(`Client ${(isUpdating?'updated':'saved')} successfully!`)

    redirect('/home/clients/' +clientId) 
}