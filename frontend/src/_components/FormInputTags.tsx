'use client'

import { XMarkIcon } from "@heroicons/react/24/solid"
import {   useState } from "react"


export default function FormInputTags(
    {
        values,
        name
    }: {
        values: string[],
        name:string
    }) {

    const [tags, setTags] = useState(values);
    const remove =(tag:string)=>{ 
        tags.splice(tags.indexOf(tag), 1);
        setTags([...tags]); 
    }
   
  

    function handleKeyDown(event: React.KeyboardEvent<HTMLInputElement>): void {

        if(event.key === 'Enter'){
            event.preventDefault(); 
            const newTag = event.currentTarget.value;
            event.currentTarget.value = '';
            setTags([...tags,newTag]); 
        }
    }

    return (
        <div className="mt-2 flex flex-wrap items-center border border-gray-300 rounded-md p-2 space-x-2">
            <div id="tags-container" className="flex flex-wrap items-center gap-2">
                {
                    tags?.map((tag,index) => { 
                        return (
                            <div key={index} className="flex items-center border border-indigo-200 focus:outline-hidden bg-indigo-100 font-sm text-gray-900 px-2 rounded-sm py-0.5 text-sm">
                                <span>{tag}</span>
                                <input type="hidden" value={tag} name={name} ></input>
                                <button type="button" 
                                    onClick={()=>remove(tag)}  
                                    className="ml-2 -ml-px  focus:z-10 " >
                                    <XMarkIcon aria-hidden="true" className=" -ml-0.5 size-5  text-gray-400 hover:text-gray-900" />
                                </button>
                            </div>
                        )
                    }) 
                }

            </div>
            <input id="tags-input"  onKeyDown={handleKeyDown} type="text" className="flex-1 border-none font-sm text-sm  outline-none focus:ring-0"></input>

        </div>
    )
}