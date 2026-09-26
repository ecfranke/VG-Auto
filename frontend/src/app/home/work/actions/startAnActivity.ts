'use server';
import { httpPost } from "@/_lib/server/query-api";
import { redirect } from "next/navigation";


export async function startAnActivity(workId: string,activityName:string) {

    const response = await httpPost({
        url: `work/${workId}/${activityName}`,
        body: {}
    });
    const activityId = await response.json();
    

    redirect(`/home/work/${workId}/${activityId}/edit/startfresh`);
}




