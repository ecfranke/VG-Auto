'use server'

import { httpPost } from "./query-api"; 

 
export async function extendSession() {
    //TODO fix
    console.log('extending session'); 
    const response = await httpPost(
        {
          url:'users/extendsession' , 
          body : {},  
        }
      )
    if(response.ok){
      //const jwt = await response.text();
      //return createSession(jwt);
    }
    return null 
  }