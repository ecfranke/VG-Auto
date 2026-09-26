import { CardHeader } from "@/_components/Card" 
import { PlusCircleIcon } from "@heroicons/react/24/outline"
import Link from "next/link"

export function SearchCardHeader({
    title,
    description,
    pageName, 
    children,
  }: {
    title?: string | undefined,
    description?:string | undefined,
    pageName?: string, 
    children?: React.ReactNode
  }){
    return (
      <div className=" sm:px-0">
      <CardHeader title={title} description={description}  >
        {children}
          <div className="mt-2  shrink-0">
            
            <Link href={`/home/${pageName}/new`}
              type="button"
              className="inline-flex items-center gap-x-1.5 rounded-md bg-white px-3 py-2 text-sm font-semibold text-gray-900 ring-1 shadow-xs ring-gray-300 ring-inset hover:bg-gray-50"
            >
              <PlusCircleIcon aria-hidden="true" className="-ml-0.5 size-5" />
              Add new
            </Link> 
            </div>
      </CardHeader>
      </div>
    )
  }