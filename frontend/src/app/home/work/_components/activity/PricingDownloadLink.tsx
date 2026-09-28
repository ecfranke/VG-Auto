 
'use client';

import { PaperClipIcon } from '@heroicons/react/20/solid'; 
import { useState } from 'react';
import Spinner from '@/_components/Spinner';
import { ArrowDownTrayIcon } from "@heroicons/react/20/solid";
import Link from 'next/link';
import PrintPricingLink from './PrintPricingLink';


/** Downloads the PDF; returns an error message when it could not be created. */
const handleFileDownload = async (pricingId: string, pricingName: string, fileName: string): Promise<string | null> => {
  try {
    const response = await fetch(`/home/pdf/${pricingName.toLowerCase()}/${pricingId}`, { cache: 'no-store' })
    if (!response.ok) {
      const json = await response.json().catch(() => null)
      return json?.error ?? `The PDF could not be created (error ${response.status}).`
    }
    const url = window.URL.createObjectURL(await response.blob())
    const link = document.createElement("a")
    link.href = url
    link.setAttribute("download", fileName)
    document.body.appendChild(link)
    link.click()
    document.body.removeChild(link)
    setTimeout(() => window.URL.revokeObjectURL(url), 10000)
    return null
  } catch {
    return 'The PDF could not be downloaded. Check the connection and try again.'
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
    const [error, setError] = useState<string | null>(null);
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
                    setError(null);
                    try {
                        setError(await handleFileDownload(id,name,fileName));
                    } finally {
                        setIsDownloading(false);
                    }
                    
                }} className="font-medium text-indigo-600 hover:text-indigo-500">
                    {!isDownloading&&clickableElement} {isDownloading&& downloadingElement}
                </Link>
            </div>
            {error && <span role="alert" className="text-xs/6 text-red-600">{error}</span>}
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