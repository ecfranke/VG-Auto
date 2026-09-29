'use server'

import { DescriptionItem } from '@/_components/DescriptionItem';
import { httpGet } from '@/_lib/server/query-api'
import Main from '../../_components/Main'; 
import DisplayOptionsMenu from '@/_components/DisplayOptionsMenu';
import { IVehicleData } from '../model';
import { CardHeader } from '@/_components/Card';



export default async function Page({
    params,
}: {
    params: Promise<{ id: string }>
}) {
    const id = (await params).id;
    const data = await httpGet('vehicles/' + id);
    const vehicle = await data.json() as IVehicleData;
  
    return (

        <Main header={
            <CardHeader  >
                 <h3 className="px-1 text-base font-semibold text-gray-900">Vehicle Information</h3>
                <DisplayOptionsMenu id={id} pageName='vehicles'></DisplayOptionsMenu>
            </CardHeader>}>
            <dl className="divide-y divide-gray-100"> 
                <DescriptionItem label='Manufacturer' value={vehicle.manufacturer}></DescriptionItem>
                <DescriptionItem label='Model' value={vehicle.model}></DescriptionItem>
                <DescriptionItem label='Trim' value={vehicle.trim ?? ''}></DescriptionItem>
                <DescriptionItem label='Year' value={vehicle.year ?? ''}></DescriptionItem>
                <DescriptionItem label='VIN' value={vehicle.vin}></DescriptionItem>
                <DescriptionItem label='License plate' value={vehicle.licensePlate}></DescriptionItem>
                <DescriptionItem label='Odometer' value={vehicle.odo}></DescriptionItem>
                <DescriptionItem label='Owner' value={vehicle.ownerName}></DescriptionItem>
                <DescriptionItem label='About' value={vehicle.description}></DescriptionItem>
            </dl>
        </Main>
    )

}