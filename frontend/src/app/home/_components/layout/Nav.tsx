'use client'
import Image from "next/image"
import ProfileMenu from "./ProfileMenu"
import { 
    Cog6ToothIcon, 
    HomeIcon,
    QueueListIcon,
    TruckIcon,
    UsersIcon, 
  } from '@heroicons/react/24/outline'
import clsx from "clsx"; 
import { usePathname } from 'next/navigation'
import ThemeSwitch, { Theme } from "@/_components/ThemeSwitch";
 const navigationIconClass = "size-6 shrink-0";
const navigation = [
    { name: 'Home', href: '/home', icon: <HomeIcon aria-hidden="true" className={navigationIconClass}></HomeIcon>},
    { name: 'Work', href: '/home/work', icon: <QueueListIcon aria-hidden="true" className={navigationIconClass}></QueueListIcon> },
    { name: 'Clients', href: '/home/clients', icon: <UsersIcon aria-hidden="true" className={navigationIconClass}></UsersIcon>  },
    { name: 'Vehicles', href: '/home/vehicles', icon: <TruckIcon aria-hidden="true" className={navigationIconClass}></TruckIcon>  },
    { name: 'Inventory', href: '/home/inventory', icon: <Cog6ToothIcon aria-hidden="true" className={navigationIconClass}></Cog6ToothIcon>  },
    // { name: 'Services', href: '/home/services', icon: <WrenchScrewdriverIcon aria-hidden="true" className={navigationIconClass}></WrenchScrewdriverIcon>  },
]
 

export default   function Nav({
    onSmallScreen, 
    fullName,
    imageUrl,
    isAdmin = false,
    theme = 'system',
}:{
    onSmallScreen:boolean, 
    fullName:string,
    imageUrl:string,
    isAdmin?:boolean,
    theme?:Theme
}) {
    const currentPath = usePathname() ; 
    return (
        <>
            <div className="flex h-16 shrink-0 items-center">
                <Image alt="VG Auto" width="50" height="50" className="h-8 w-auto" src="/logo.png" ></Image>
                <span className="ml-3 text-base font-semibold text-white">VG Auto</span>
            </div>
            <nav className="flex flex-1 flex-col">
                <ul role="list" className="flex flex-1 flex-col gap-y-7">
                    <li>
                        <ul role="list" className="-mx-2 space-y-1">
                            {navigation.map((item) => (
                                <li key={item.name}>
                                    <a
                                        href={item.href}
                                        className={clsx(
                                               (item.href !=='/home'  &&currentPath?.startsWith(item.href) || item.href =='/home'&& currentPath === '/home') //home is ambigous
                                                ? 'bg-white/10 text-white [&>svg]:text-blue-400'
                                                : 'text-slate-400 hover:bg-white/5 hover:text-white',
                                            'group flex gap-x-3 rounded-md p-2 text-sm/6 font-semibold',
                                        )}
                                    >
                                        {item.icon}
                                        {item.name}
                                    </a>
                                </li>
                            ))}
                        </ul>
                    </li>
                    {!onSmallScreen && <li className="mt-auto flex flex-col mb-5   ">
                        <a
                            href="/home/settings"
                            className={clsx(currentPath?.startsWith('/home/settings') ? 'bg-white/10 text-white [&>svg]:text-blue-400' : 'text-slate-400 hover:bg-white/5 hover:text-white',
                                "group -mx-2 flex gap-x-3 rounded-md p-2 text-sm/6 font-semibold")}
                        >
                            <Cog6ToothIcon aria-hidden="true" className="size-6 shrink-0" />
                            Settings
                        </a>
                        <ThemeSwitch initial={theme} className="my-3 self-start" />
                        <ProfileMenu  fullName={fullName} imageUrl={imageUrl} isAdmin={isAdmin} onSmallScreen={false}></ProfileMenu>
                    </li>}
                    {onSmallScreen && <li className="mt-auto mb-5"><ThemeSwitch initial={theme} /></li>}
                </ul>
            </nav>

        </>
    )
}