import { downloadPricing } from "@/app/home/work/actions/downloadPricing";
import '@/_styles/print.css'; 
import PrintPage from "./Print";

 

export default async function Page({
    params,
}: {
    params: Promise<{ slug: string[] }>
}) {

    const [pricing,id] = (await params).slug;
    const pricingHtml = (await downloadPricing({ pricingId:id, pricingName:pricing, downloadHtml:true })) as string;
   
  return (
    <PrintPage pricingHtml={pricingHtml} />
  );
}

