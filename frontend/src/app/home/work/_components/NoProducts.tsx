import { PlusIcon } from "@heroicons/react/24/outline";
import Link from "next/link";
import { IWorkData } from "../model";

export default function NoProducts({
    work, 
    activityId
}: {
    work: IWorkData, 
    activityId: string
}){
    const editUrl = `/home/work/${work.id}/${activityId}/edit/startfresh`;
    return ( <div className="text-center">
                            
        <h3 className="mt-2 text-sm font-semibold text-gray-900">No products or services yet</h3> 
       {!work.issuance&& <div className="my-6">
          <Link href={editUrl}
            type="button"
            className="inline-flex items-center rounded-md bg-indigo-600 px-3 py-2 text-sm font-semibold text-white shadow-xs hover:bg-indigo-500 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-indigo-600"
          >
            <PlusIcon aria-hidden="true" className="mr-1.5 -ml-0.5 size-5" />
            Start
          </Link>
        </div>}
      </div>)
}