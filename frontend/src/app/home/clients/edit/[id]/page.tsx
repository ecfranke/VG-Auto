'use server'
 
import { httpGet  } from '@/_lib/server/query-api';
import ClientInput from '../../_components/ClientInput'; 
import { createOrUpdate } from '../../createOrUpdate'; 
import { IClientData } from '../../model';
import Main from '@/app/home/_components/Main';
import { CardHeader } from '@/_components/Card';
 

export default async function Page({
    params,
}: {
    params: Promise<{ id: string }>
}) {
 
 
 const id = (await params).id;
 const data = await httpGet('clients/' + id); 
 const client = await data.json() as IClientData;
  
    return (
        <Main header={
                <CardHeader title='Client Information' description='Edit details'  >  
                </CardHeader>}>
                <form   action={createOrUpdate}>
                  <input type="hidden" name='id' value={id}></input>
                    <ClientInput  client={client}></ClientInput>
                </form>
         </Main> 
    )
}
