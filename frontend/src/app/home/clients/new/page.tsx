'use server'

import ClientInput from '../_components/ClientInput';
import { createOrUpdate } from '../createOrUpdate';
import Main from '../../_components/Main'; 
import { CardHeader } from '@/_components/Card';

export default async function Page() {
  
    return (
        <Main header={
                        <CardHeader title='New client' description='Enter details'  >  
                        </CardHeader>}>
                <form action={createOrUpdate}>
                    <input type="hidden" name='id' ></input>
                    <ClientInput  ></ClientInput>
                </form> 
        </Main> 
    )
}
