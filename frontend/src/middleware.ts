import { NextResponse, NextRequest } from 'next/server' 
import authorizationMiddleware from './_lib/server/authorization-middleware'; 
import searchParamsMiddleware from './_lib/server/searchparams-middleware';
 
export default async function middleware(request: NextRequest) {
  
  let nextResponse = NextResponse.next(); 
  nextResponse = await searchParamsMiddleware(request,nextResponse);
  nextResponse = await authorizationMiddleware(request,nextResponse);
  return nextResponse;
}

export const config = {
  matcher: ['/home/:path*', '/auth/:path*'],
}