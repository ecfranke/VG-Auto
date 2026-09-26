'use server';
import { httpDelete } from "@/_lib/server/query-api";
import { redirect } from "next/navigation";


export async function deleteAnActivity(workId: string, activityId: string,activityName: string) {

    const response = await httpDelete({
        url: `work/${workId}/${activityName}/${activityId}`,
        body: {}
    });
    await response.text();

    redirect(`/home/work/${workId}`);
}

export async function deleteWork(workId: string) {

    const response = await httpDelete({
        url: `work`,
        body: [workId]
    });
    await response.text();

    redirect(`/home/work`);
}

