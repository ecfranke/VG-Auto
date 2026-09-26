import { httpGet } from "@/_lib/server/query-api";
import { IUserProfile } from "./model";
import SettingsTabs from "@/_components/SettingsTabs";
import Main from "../_components/Main";
import Link from "next/link";
import { DescriptionItem } from "@/_components/DescriptionItem";
import Image from 'next/image';

export default async function Page() {

    const data = await httpGet('profile');
    const options = await data.json() as IUserProfile;
 
    return ( 
        <Main header={
            <SettingsTabs>
            </SettingsTabs>
        } narrow={true}>
              
            <div className=" px-0 ">
                <h3 className="text-base/7 font-semibold text-gray-900 my-4">My information</h3> 
               {options.profileImageBase64&&<Image
                      alt={`${options.firstName} ${options.lastName}`}
                      src={decodeURIComponent(encodeURIComponent("data:image/png;base64, " + options.profileImageBase64))}
                      width={100}
                      height={100}
                      className="size-24 flex-none rounded-lg bg-gray-800 object-cover"
                      ></Image>} 
            </div>
            <div className="mt-6 border-t border-gray-100">
            <dl className="divide-y divide-gray-100">
                    <DescriptionItem label='Full name' value={options.firstName+` `+options.lastName}></DescriptionItem>
                    <DescriptionItem label='User name' value={options.userName}></DescriptionItem>
                    <DescriptionItem label='Email' value={options.email}></DescriptionItem>
                </dl>
            </div> 
             <div className="mt-6 flex items-center justify-end gap-x-6">
                <Link href={`/home/profile/edit`}
                    type="button"
                    className="inline-flex items-center gap-x-1.5 rounded-md bg-indigo-600 px-3 py-2 text-sm font-semibold text-white shadow-xs hover:bg-indigo-500 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-indigo-600"
                >
                    Edit
                </Link>
            </div>
        </Main>
    )

}
