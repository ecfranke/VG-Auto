'use client'

import { useEffect } from "react";

const zoomOutMobile = () => {
    const viewport = document.querySelector('meta[name="viewport"]') as HTMLMetaElement;

    if ( viewport ) {
      viewport.content = 'initial-scale=0.2';
      viewport.content = 'width=device-width';
    }
  }
export default function PrintPage({
    pricingHtml
}: {
    pricingHtml: string
}){

      const markup = { __html: pricingHtml };
      useEffect(() => {
     zoomOutMobile();
       window.print(); 
    }, 
     []);
    return <div dangerouslySetInnerHTML={markup} />
}