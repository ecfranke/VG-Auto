'use client'

import { usePathname, useRouter } from "next/navigation"; 
import { IActivities, IOfferIssuance, IWorkData } from "../model";
import Select from "@/_components/Select";
import { getActivityDisplayName } from "./activity/getActivityDisplayName";

export default function ActivitySelect({
    work,
    activities,
    issueances,
}: {
    work: IWorkData,
    activities: IActivities,
    issueances: IOfferIssuance[]
}) {

    const router = useRouter();
    const currentPath = usePathname(); 
    const items = activities.items ?? [];

    return (
        <>
            <div className=" 2xl:hidden  grid grid-cols-1">
                {
                    items.length > 1 && <Select
                        defaultValue={currentPath}
                        onChange={e => {
                            router.push(e.currentTarget.value);
                        }} >
                        {
                            items.map(item => {
                                const id = item.id;
                                const issuance = issueances.find(x => x.id === item.id);
                                const name = getActivityDisplayName(item.name, item.number, issuance?.number);
                                const href = `/home/work/${work.id}/${item.id}`;
                                return (
                                    <option value={href} key={id}>{name}</option>
                                )
                            })
                        }
                    </Select>
                }
            </div>
        </>
    )
}