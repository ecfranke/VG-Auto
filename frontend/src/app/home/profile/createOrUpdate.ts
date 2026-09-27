'use server'

import { httpDelete, httpPut } from "@/_lib/server/query-api";
import {  pushToast } from "@/_lib/server/pushToast";
import { redirect } from "next/navigation";  
import { createSession } from "@/_lib/server/session";


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

  // the API returns fresh tokens after a password change
  const tokens = await response.json();
  if (tokens?.jwt && tokens?.publicJwt) {
    await createSession(tokens.jwt, tokens.publicJwt);
  }

  pushToast(`Password updated successfully!`)

  redirect('/home/profile')
}


export async function unlinkExternalLogin(formData: FormData) {
  const provider = formData.get('provider')?.toString() ?? '';
  await httpDelete({ url: `profile/externallogins/${encodeURIComponent(provider)}`, body: null });
  pushToast(`Microsoft account unlinked.`);
  redirect('/home/profile/edit');
}
