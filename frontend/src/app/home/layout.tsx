'use server'
 
 
import { cookies } from 'next/headers';
import Nav from './_components/layout/Nav'
import NavDialog from './_components/layout/NavDialog'
import ToastMessages from '@/_components/ToastMessages'  
import { redirect } from 'next/navigation';
import { jwtDecode } from 'jwt-decode';
import { currentTheme } from '@/_lib/server/theme';

interface CustomJwtPayload {
    FullName?: string; 
    vg_role?: string;
  }
export default async function Layout({ children }: { children: React.ReactNode }) {
    
    const jwt = (await cookies()).get('jwt')?.value;
    
    if(!jwt) {
        redirect('/home/logout'); 
    }
    
    // Decode the JWT to get the claims
    const decodedToken = jwtDecode<CustomJwtPayload>(jwt);
    const fullName = decodedToken.FullName || ''; // Extract the FullName claim
    // display only; the API checks the role for every administrative request
    const isAdmin = decodedToken.vg_role === 'admin' || decodedToken.vg_role === 'superadmin';
    
    // If there's no full name in the token, you might want to redirect or handle it
    if(!fullName) {
        redirect('/home/logout');
    }

    const imageUrl = `${process.env.NEXT_PUBLIC_API_URL}/api/users/profilepicture/${jwt}`
    const theme = await currentTheme();
    return (
        <>
            {/* <Timeout></Timeout> */}
            <ToastMessages></ToastMessages>
            <div>
                {/* Static sidebar for desktop */}
                <div className="hidden lg:fixed lg:inset-y-0 lg:z-50 lg:flex lg:w-62 lg:flex-col">
                    {/* Sidebar component, swap this element with another sidebar if you like */}
                    <div className="flex grow flex-col gap-y-5 overflow-y-auto bg-slate-950 px-6 dark:border-r dark:border-white/10">
                      <Nav  imageUrl={imageUrl} fullName={fullName} isAdmin={isAdmin} onSmallScreen={false} theme={theme}></Nav>   
                    </div>
                </div>
                 <NavDialog imageUrl={imageUrl} fullName={fullName} isAdmin={isAdmin} theme={theme}></NavDialog>   
                {children}
              
              </div>
        </>
    )
}
