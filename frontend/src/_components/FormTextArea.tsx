import { ChangeEvent } from "react";


interface ITextAreaOnChange {
    (event: ChangeEvent<HTMLTextAreaElement>): void
}

export default function FormTextArea({
    name,
    label,
    defaultValue, 
    value,
    rows = 3,
    placeholder,
    onInputChange,
}: {
    name: string,
    label?: string | undefined,
    rows?: number|undefined,
    defaultValue?: string | undefined, 
    value?: string | undefined, 
    placeholder?: string| undefined,
    onInputChange?: ITextAreaOnChange
}) {
 
    
    return (
        <>
            {label&&<label htmlFor={name} className="block text-sm/6 font-medium text-gray-900">
                {label}
            </label>}
            <div className="mt-2">
                <textarea
                    id={name}
                    name={name}
                    rows={rows}
                    value={value}
                    placeholder={placeholder}
                    onChange={onInputChange}
                    className="block w-full rounded-md bg-white px-3 py-1.5 text-base text-gray-900 outline-1 -outline-offset-1 outline-gray-300 placeholder:text-gray-400 focus:outline-2 focus:-outline-offset-2 focus:outline-indigo-600 text-sm/6"
                    defaultValue={defaultValue}
                />
            </div> 
        </>
    )
}