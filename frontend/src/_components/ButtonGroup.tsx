'use client'

import Link from "next/link"
import { Menu, MenuButton, MenuItem, MenuItems } from '@headlessui/react'
import { ChevronDownIcon } from '@heroicons/react/20/solid'
import clsx from "clsx"; 

export interface IButtonOption{
    name:string,
    href?:string|undefined,
    onClick?:()=>void| Promise<void>| undefined,
    inMenu?:boolean|undefined,
    isPrimary?:boolean|undefined,
    redText?: boolean|undefined 
}

export default function ButtonGroup({
    options
}:{
    options:IButtonOption[]
}){
    
    const normalButtons = options.filter(x => !x.inMenu);
        const menuButtons = options.filter(x=>x.inMenu);

        if(options.length==0) return <></>
        
        return <div className="inline-flex  ml-2  rounded-md shadow-xs">
        {normalButtons.map((opt,index) => {
            
            const className = clsx(
                 index==0&&"rounded-l-md", 
                 index==(normalButtons.length-1)&&menuButtons.length==0&&"rounded-r-md", 
                    opt.isPrimary ? "relative inline-flex items-center  -ml-px bg-indigo-600 px-3 py-2 text-sm font-semibold text-white   hover:bg-indigo-500 focus:z-10" 
                                    :clsx(opt.redText?"text-red-900 ring-red-300 hover:bg-red-50":"text-gray-900 ring-gray-300 hover:bg-gray-50", "relative  inline-flex items-end  -ml-px bg-white px-3 py-2 text-sm font-semibold  ring-1 ring-inset  focus:z-10"))

            return opt.href ?
                <Link key={index} href={opt.href} className={className}>{opt.name}</Link> :
                <button key={index} type={(!opt.onClick ? "submit" : "button")} onClick={opt.onClick} className={className}>{opt.name}</button>
        })}
        {menuButtons.length>0 && 
          <Menu as="div" className="relative -ml-px block">
            <MenuButton className="relative  inline-flex items-end rounded-r-md bg-white px-2 py-2 text-gray-400 ring-1 ring-gray-300 ring-inset hover:bg-gray-50 focus:z-10">
                <span className="sr-only">Open options</span>
                <ChevronDownIcon aria-hidden="true" className="size-5" />
            </MenuButton>
            <MenuItems
                transition
                modal={false}
                className=" absolute bottom-full  right-0 z-20 mb-2 -mr-1 w-56 origin-top-right rounded-md bg-white ring-1 shadow-lg ring-black/5 transition focus:outline-hidden data-closed:scale-95 data-closed:transform data-closed:opacity-0 data-enter:duration-100 data-enter:ease-out data-leave:duration-75 data-leave:ease-in">
                <div className="py-1 z-20">
                    {options.filter(x => x.inMenu).map((opt,index) => {
                        const className = clsx(opt.redText?"text-red-700   data-focus:text-red-900":"text-gray-700   data-focus:text-gray-900", "block z-20 px-4 py-2 text-sm  data-focus:outline-hidden") 
                        return (
                            <MenuItem key={index}>
                               {opt.href ?
                                    <Link href={opt.href} className={className}>{opt.name}</Link> :
                                    <button type={(!opt.onClick ? "submit" : "button")} onClick={opt.onClick} className={className}>{opt.name}</button> 
                                }
                            </MenuItem>
                        )
                    })}
                </div>
            </MenuItems>
        </Menu>} 
    </div>
}