import { ChevronDownIcon } from '@heroicons/react/20/solid' 
import { ChangeEvent } from 'react'

export interface ISelectOnChange {
    (event: ChangeEvent<HTMLSelectElement>): void
}
 
export default function Select({
    id,
    name,
    defaultValue,
    value,
    onChange,
    children,
}:{
    id?: string | undefined,
    name?: string | undefined ,
    defaultValue?: string | undefined ,
    value?: string | number | undefined ,
    onChange?: ISelectOnChange,
    children?: React.ReactNode
}){
      
    return (
      <>
        <select
        id={id}
        name={name} 
        defaultValue={defaultValue}
        value={value}
        onChange={onChange }
        className="col-start-1 row-start-1 w-full appearance-none rounded-md bg-white py-1.5 pr-8 pl-3 text-base text-gray-900 outline-1 -outline-offset-1 outline-gray-300 focus:outline-2 focus:-outline-offset-2 focus:outline-indigo-600 text-sm/6"
    >
        {children}
    </select>
    <ChevronDownIcon
        aria-hidden="true"
        className="pointer-events-none col-start-1 row-start-1 mr-2 size-5 self-center justify-self-end text-gray-500 sm:size-4"
    />
    </>
    )
}