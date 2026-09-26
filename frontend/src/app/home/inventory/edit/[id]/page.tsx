'use server'
 
import { httpGet  } from '@/_lib/server/query-api';
import InventoryInput from '../../_components/InventoryInput'; 
import { createOrUpdate } from '../../createOrUpdate';
import Main from '@/app/home/_components/Main';
import { ILocation, ISparepartData } from '../../model';
import { CardHeader } from '@/_components/Card';
 

export default async function Page({
    params,
}: {
    params: Promise<{ id: string }>
}) {
 
 
 const id = (await params).id;
 const data = await httpGet('spareparts/' + id); 
 const sparepart = await data.json() as ISparepartData;

  const storages = await httpGet('storages');
     const locations = await storages.json() as ILocation[]; 
     
  
    return (
        <Main header={
                        <CardHeader title='Spare part Information' description='Edit details' >  
                        </CardHeader>}>
                <form action={createOrUpdate}>
                  <input type="hidden" name='id' value={id}></input>
                    <InventoryInput allLocations={locations} sparepart={sparepart}></InventoryInput>
                </form>
         </Main> 
    )
}
