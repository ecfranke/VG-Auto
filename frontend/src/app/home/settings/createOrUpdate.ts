'use server'

import { httpPut } from "@/_lib/server/query-api";
import {  pushToast } from "@/_lib/server/pushToast";
import { redirect } from "next/navigation"; 
import { IUserOptions } from "./model";
import { taxesFromForm } from "@/_lib/shared/taxes";


export async function createOrUpdate(
    formData: FormData
    ) {
       
    const parsed = taxesFromForm(formData);
    if ('error' in parsed) {
      await pushToast(parsed.error, true);
      redirect('/home/settings/edit');
    }
    const taxes = parsed.taxes;
    const vatRate = Math.round(taxes.tax1Rate + taxes.tax2Rate);
    const signatureLine = formData.get('signatureLine') == 'on';
    const body = {
      requisites: {
        name: formData.get('name'),
        phone: formData.get('phone'),
        address:formData.get('address'),
        email: formData.get('email'),
        bankAccount: formData.get('bankAccount'),
        regNr: formData.get('regNr'),
        kmkr: formData.get('kmkr')
      },
      pricing: {
        invoice: {
          vatRate: vatRate,
          surCharge: formData.get('surCharge'),
          disclaimer: formData.get('disclaimer'),
          signatureLine: signatureLine,
          emailContent: formData.get('emailContent')
        },
        estimate: {
          emailContent: formData.get('estimateEmailContent')
        },
        currency: formData.get('currency')?.toString() || null,
        taxes,
      }
    }  as IUserOptions;
   
    const response =  await httpPut({url:'options',body}) 

     await response.text();
        
    pushToast(`Options updated successfully!`)

    redirect('/home/settings') 
}
