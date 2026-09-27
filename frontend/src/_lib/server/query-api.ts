'server-only'
import { redirect } from "next/navigation";
import { getJwt } from "./session"; 
import { headers } from "next/headers";
import { pushToast } from "./pushToast";

interface IAPICall
{
  url:string,
  authorize? : boolean,
  body?: any | null, // eslint-disable-line @typescript-eslint/no-explicit-any
  method: string,
  /** return non-OK responses to the caller instead of redirecting to the error page */
  raw?: boolean
}

/** Client address of the current request, forwarded so the API can rate limit per user. */
async function clientAddress(): Promise<string | null> {
  try {
    const h = await headers();
    // set by our nginx config from $remote_addr; otherwise take the hop added by the nearest proxy
    const realIp = h.get('x-real-ip');
    if (realIp) return realIp.trim();
    const forwarded = h.get('x-forwarded-for');
    if (forwarded) return forwarded.split(',').pop()!.trim();
    return null;
  } catch {
    return null;
  }
}

async function apiCall({
  url,
  authorize=true,
  method,
  body =null,
  raw = false
}:IAPICall) { 
  const  requestHeaders:Record<string,string> =   {
   "Content-Type": "application/json",
  };
  if(authorize) { 
    const jwt = await getJwt(); 
    if (jwt) requestHeaders["Authorization"] =  'Bearer ' + jwt;
  } 
  const ip = await clientAddress();
  if (ip) requestHeaders["X-Forwarded-For"] = ip;
  const fullUrl = process.env.API_URL +`/api/${url}`;
  const request = {
    method,
    headers: requestHeaders,
    body : body? JSON.stringify(body):null
  };
   
  const response = await fetch(fullUrl,request);
  if (!response.ok && !raw) {
    const responseText = await response.text();
    // never log request bodies or headers: they contain passwords and tokens
    console.log(`API ${method} ${url} failed with ${response.status}: ${responseText.substring(0, 500)}`);
    const hasContentType = response.headers.has('Content-Type');
    let message = 'API Error occurred server side';
    let isUserError = false;
    if(hasContentType){
      const contentType = response.headers.get('Content-Type');
      if(contentType?.startsWith('application/json'))
      {
        const responseJson = JSON.parse(responseText);
        if (responseJson.passwordChangeRequired) {
          redirect('/auth/change-password');
        }
        if (responseJson.exceptionMessage) {
            message = responseJson.exceptionMessage;
        }
        if(responseJson.isUserError)
          {
            isUserError = true;
          }
      } 
    }
  
    if(isUserError)
      {
        const headersList = await headers()
        const currentPath = headersList.get('currentPath')
  
        if(currentPath){
          pushToast(message,true);
          redirect(currentPath)  
        }
      }

     redirect(`/error?code=${response.status}&statusText=${encodeURIComponent(response.statusText)}&text=${encodeURIComponent(message)}`)
  }
  return response;
}

export async function httpGet(url: string) {
    return apiCall({
      url,
      method:"GET"
    }); 
}

export async function httpDelete({
  url,
  body
}:{
  url:string,
  body:any,// eslint-disable-line @typescript-eslint/no-explicit-any 
}) {
  return apiCall({
    url,
    method:"DELETE", 
    body, 
  }); 
}

export async function httpPost({
  url,
  body, 
  authorize=true, 
  raw=false,
}:{
  url:string,
  body:any,// eslint-disable-line @typescript-eslint/no-explicit-any
  authorize?: boolean | undefined 
  raw?: boolean | undefined
}) {
  return apiCall({
    url,
    method:"POST",
    authorize,
    body, 
    raw,
  }); 
}

export async function httpPut({
  url,
  body, 
  authorize=true, 
  raw=false,
}:{
  url:string,
  body:any,// eslint-disable-line @typescript-eslint/no-explicit-any
  authorize?: boolean | undefined
  verboseLog?: boolean | undefined
  raw?: boolean | undefined
}) {
  return apiCall({
    url,
    method:"PUT",
    authorize,
    body, 
    raw,
  }); 
}