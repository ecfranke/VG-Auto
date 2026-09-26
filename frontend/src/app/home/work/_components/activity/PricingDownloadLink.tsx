 
'use client';

import { PaperClipIcon } from '@heroicons/react/20/solid'; 
import { downloadPricing } from '../../actions/downloadPricing';
import { useState } from 'react';
import Spinner from '@/_components/Spinner';
import { ArrowDownTrayIcon } from "@heroicons/react/20/solid";
import Link from 'next/link';
import PrintPricingLink from './PrintPricingLink';


const handleFileDownload = async (pricingId:string, pricingName: string,fileName: string) => {
    try {
      const blob = await downloadPricing({
        pricingId,pricingName
      });
     // Create a temporary anchor element to trigger the download
     const url = window.URL.createObjectURL(new Blob([blob]));
     const link = document.createElement("a");
     link.href = url;
     // Setting filename received in response
     link.setAttribute("download", fileName);
     document.body.appendChild(link);
     link.click();
     document.body.removeChild(link);

    } catch (error) {
      console.log("Error", error)
    }
  }

export default function PricingDownloadLink({
    id,
    name,
    number,
    downloadingElement= <>{<Spinner></Spinner>}</>,
    hidePaperClip = true,
    hideLabel,
    clickableElement=<>{<ArrowDownTrayIcon aria-hidden="true" className="h-6 w-5 text-gray-400" ></ArrowDownTrayIcon>}</>, 
}:{
    id:string,
    name:string,
    number:string | number ,
    clickableElement?: React.ReactNode,
    downloadingElement?: React.ReactNode,
    hidePaperClip?: boolean,
    hideLabel?:boolean
}) {
    
    const [isDownloading,setIsDownloading] = useState(false);
    const fileName = `${name.toLowerCase()}_nr_${number}.pdf`;

    
    return (
        <div className="flex  ">
       {!hidePaperClip&&   <PaperClipIcon aria-hidden="true" className="h-6 w-5 text-gray-400 mr-4" />}
        <div className=" flex min-w-0 flex-1 gap-2">
          {!hideLabel&& <span className="truncate text-sm/6 font-bold">{  `${name} nr. ${number}`}</span> }
            <div className=" text-sm/6 text-gray-500">
                <Link href="#"  onClick={async (e)=>{
                   e.preventDefault();
                    setIsDownloading(true);
                    const promise =  handleFileDownload(id,name,fileName);
                    
                    promise.finally(()=>{
                        //scroll stuff
                        setIsDownloading(false);
                    })
                    await promise; 
                    
                }} className="font-medium text-indigo-600 hover:text-indigo-500">
                    {!isDownloading&&clickableElement} {isDownloading&& downloadingElement}
                </Link>
            </div>
            <div className=" text-sm/6 text-gray-500">
               <PrintPricingLink id={id} pricingName={name}></PrintPricingLink>
            </div>
        </div>
    </div> 
    )
}

export {
  handleFileDownload
}