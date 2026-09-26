import { EllipsisVerticalIcon } from '@heroicons/react/20/solid'
import { IButtonOption } from "./ButtonGroup"; 
import { Menu, MenuButton, MenuItems, MenuItem } from '@headlessui/react';
import Link from 'next/link';

export default function HamburgerMenu({
    options
}:{
    options: IButtonOption[]
}){
     const menuItemClassName = "block px-3 py-1 text-sm/6 text-gray-900 data-focus:bg-gray-50 data-focus:outline-hidden";
     const redMenuItemClassName = "block px-3 py-1 text-sm/6 text-red-900 data-focus:bg-red-50 data-focus:outline-hidden";
    const  className="absolute right-0 z-10 mt-2 w-32 origin-top-right rounded-md bg-white py-2 ring-1 shadow-lg ring-gray-900/5 transition focus:outline-hidden data-closed:scale-95 data-closed:transform data-closed:opacity-0 data-enter:duration-100 data-enter:ease-out data-leave:duration-75 data-leave:ease-in";
    if(options.length==0) return <></>
    return (
         <Menu as="div" className="relative flex-none">
            <MenuButton className="-m-2.5 block pb-2.5 text-gray-500 hover:text-gray-900">
                <span className="sr-only">Open options</span>
                <EllipsisVerticalIcon aria-hidden="true" className="size-5" />
            </MenuButton>
            <MenuItems
            modal={false}
                transition
                className={className}
            >
                {options.map((item) => (
                    <MenuItem key={item.name}> 
                        {item.href ?
                             <Link href={item.href} className={menuItemClassName}>{item.name}</Link> :
                               <button type={(!item.onClick ? "submit" : "button")} onClick={item.onClick} className={item.redText?redMenuItemClassName:menuItemClassName}>{item.name}</button> 
                        }
                    </MenuItem>
                ))}
            </MenuItems>
        </Menu>
    )
}