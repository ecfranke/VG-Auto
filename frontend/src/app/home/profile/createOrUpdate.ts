'use server'

import { httpPut } from "@/_lib/server/query-api";
import {  pushToast } from "@/_lib/server/pushToast";
import { redirect } from "next/navigation";  


export async function createOrUpdate(
    formData: FormData
    ) {
        
     
     const body = {  
      "firstName": formData.get('firstName'), 
      "lastName": formData.get('lastName'), 
      "email": formData.get('email'), 
      "userName": formData.get('userName'), 
      "profileImageBase64": formData.get('profileImageBase64'),  }
        
    const response =  await httpPut({url:'profile',body}) 

    await response.text();
        
    pushToast(`Profile updated successfully!`)

    redirect('/home/profile') 
}

export async function changePassword(
  formData: FormData
  ) {
    
  const body = Object.fromEntries(formData) ;
 
  const response = await httpPut({ url: 'profile/changepassword', body })

  await response.text();

  pushToast(`Password updated successfully!`)

  redirect('/home/profile')
}

