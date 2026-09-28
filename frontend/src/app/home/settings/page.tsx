import { httpGet } from "@/_lib/server/query-api";
import { IUserOptions } from "./model";
import SettingsTabs from "@/_components/SettingsTabs";
import Main from "../_components/Main";
import Link from "next/link";
import { DescriptionItem } from "@/_components/DescriptionItem";
import { sendTestEmail } from "./testEmail";
import { currentAccount } from "@/_lib/server/account";
import { describeTaxes, ITaxCountry } from "@/_lib/shared/taxes";

export default async function Page() {

    const data = await httpGet('options');
    const options = await data.json() as IUserOptions;
    const { isAdmin } = await currentAccount();
    const countries = await (await httpGet('options/taxregions')).json() as ITaxCountry[];
    const taxes = options.pricing.taxes;
    const country = countries.find(c => c.code === taxes?.country);
    const region = country?.regions.find(r => r.code === taxes?.region);
 
    return (

        <Main header={
            <SettingsTabs>
            </SettingsTabs>
        } narrow={true}>
              
            <div className="  px-0">
                <h3 className="text-base/7 font-semibold text-gray-900  my-4">Company information</h3> 
            </div>
            <div className="mt-6 border-t border-gray-100">
                <dl className="divide-y divide-gray-100">
                    <DescriptionItem label='Name' value={options.requisites.name}></DescriptionItem>
                    <DescriptionItem label='Phone' value={options.requisites.phone}></DescriptionItem>
                    <DescriptionItem label='Address' value={options.requisites.address}></DescriptionItem>
                    <DescriptionItem label='Email' value={options.requisites.email}></DescriptionItem>
                    <DescriptionItem label='Bank account' value={options.requisites.bankAccount}></DescriptionItem>
                    <DescriptionItem label='Reg No' value={options.requisites.regNr}></DescriptionItem>
                    <DescriptionItem label='Tax ID' value={options.requisites.kmkr}></DescriptionItem>
                </dl>
            </div>
            <div className=" pt-8   px-0">
                <h3 className="text-base/7 font-semibold text-gray-900">Invoice options</h3> 
            </div>
            <div className="mt-6 border-t border-gray-100">
                <dl className="divide-y divide-gray-100">
                    <DescriptionItem label='Currency' value={options.pricing.currency}></DescriptionItem>
                    <DescriptionItem label='Registered in' value={[region?.name ?? taxes?.region, country?.name].filter(Boolean).join(', ') || '—'}></DescriptionItem>
                    <DescriptionItem label='Taxes' value={describeTaxes(taxes) + ' (prices are before tax)'}></DescriptionItem>
                    <DescriptionItem label='Surcharge' value={options.pricing.invoice.surCharge}></DescriptionItem>
                    <DescriptionItem label='Disclaimer' className="whitespace-pre-line" value={options.pricing.invoice.disclaimer}></DescriptionItem>
                    <DescriptionItem label='Signature line' value={(options.pricing.invoice.signatureLine?'Yes':'No')}></DescriptionItem>
                    <DescriptionItem label='Email content'  className="whitespace-pre-line" value={options.pricing.invoice.emailContent}></DescriptionItem> 
                </dl>
            </div>
            <div className=" pt-8   px-0">
                <h3 className="text-base/7 font-semibold text-gray-900">Offer options</h3>
                <p className="mt-1 max-w-2xl text-sm/6 text-gray-500">Offer options</p>
            </div>
            <div className="mt-6 border-t border-gray-100">
                <dl className="divide-y divide-gray-100"> 
                    <DescriptionItem label='Email content' className="whitespace-pre-line" value={options.pricing.estimate.emailContent}></DescriptionItem> 
                </dl>
            </div>
            {isAdmin && <div className=" pt-8   px-0">
                <h3 className="text-base/7 font-semibold text-gray-900">Email delivery</h3>
                <p className="mt-1 max-w-2xl text-sm/6 text-gray-500">Estimates, invoices and login codes are sent through the transport configured on the server (SMTP or Microsoft Graph).</p>
            </div>}
            {isAdmin && <form action={sendTestEmail} className="mt-4 flex items-center gap-x-3">
                <input name="testEmailTo" type="email" required placeholder="you@example.com" defaultValue={options.requisites.email}
                    className="block w-full max-w-xs rounded-md bg-white px-3 py-1.5 text-sm text-gray-900 outline-1 -outline-offset-1 outline-gray-300 focus:outline-2 focus:-outline-offset-2 focus:outline-indigo-600" />
                <button type="submit" className="rounded-md bg-white px-3 py-2 text-sm font-semibold text-gray-900 shadow-xs ring-1 ring-gray-300 ring-inset hover:bg-gray-50">
                    Send test email
                </button>
            </form>}
             <div className="mt-6 flex items-center justify-end gap-x-6">
                <Link href={`/home/settings/edit`}
                    type="button"
                    className="inline-flex items-center gap-x-1.5 rounded-md bg-indigo-600 px-3 py-2 text-sm font-semibold text-white shadow-xs hover:bg-indigo-500 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-indigo-600"
                >
                    Edit
                </Link>
            </div>
        </Main>
    )

}
