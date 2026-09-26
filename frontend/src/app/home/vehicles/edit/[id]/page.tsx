'use server'
 
import { httpGet  } from '@/_lib/server/query-api';
import VehicleInput from '../../_components/VehicleInput'; 
import { createOrUpdate } from '../../createOrUpdate';
import Main from '@/app/home/_components/Main';
import { IVehicleData } from '../../model'; 
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
                <CardHeader title=' Vehicle Information' description='Edit details' >  
                </CardHeader>}>
                <form action={createOrUpdate}>
                  <input type="hidden" name='id' value={id}></input>
                    <VehicleInput  vehicle={vehicle}></VehicleInput>
                </form>
         </Main> 
    )
}
