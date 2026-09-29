 
import {  PrinterIcon } from '@heroicons/react/20/solid';  
import Link from 'next/link';

 
export default function PrintPricingLink({
    id,
    pricingName, 
}:{
    id:string ,
    pricingName:string,
}) {
    
     
    return (
      <Link href={`/print/${pricingName}/${id}`} target='_blank'  className="font-medium text-link hover:text-link-hover">
        <PrinterIcon aria-hidden="true" className="h-6 w-5 text-gray-400" ></PrinterIcon>
     </Link>
    )
} 