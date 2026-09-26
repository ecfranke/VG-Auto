import React from "react"

 function FormListItem({ 
    children,
}:{ 
    children: React.ReactNode
}){ 
    return (
        <li className="flex items-center justify-between py-4 pr-5 pl-4 text-sm/6">
            {children}
        </li>
    )
}

interface IRenderItem<T>{
    (item:T,index:number):React.ReactNode
}

export default function FormList<T>(
   {
    items ,
    children,
    renderItem
   }:{
    items?: T[] |undefined,
    children?: React.ReactNode,
    renderItem:IRenderItem<T>
   }
){
    
    return (
        <dd className="mt-0 text-sm/6 text-gray-700 sm:col-span-2 sm:mt-0">
        {items&&items.length>0&&
         <ul role="list" className="divide-y divide-gray-100 rounded-md border border-gray-200">
            {
                items?.map((item,index) => {  
                    return (
                        <FormListItem  key={item+' '+index}>
                             {renderItem(item,index) }
                             {children}
                        </FormListItem>
                    ) 
                })
            }
        </ul>} 
    </dd>
    )
}