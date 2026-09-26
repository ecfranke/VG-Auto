 
import { NextResponse, NextRequest } from 'next/server'
export default async function searchParamsMiddleware(request: NextRequest,response: NextResponse) {
  const searchParams = request.nextUrl.searchParams.toString();
  response.headers.set("searchParams", searchParams);
  response.headers.set("currentPath", request.nextUrl.pathname);
  return response;     
}
   