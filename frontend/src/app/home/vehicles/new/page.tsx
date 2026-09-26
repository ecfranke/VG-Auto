'use server'

import VehicleInput from '../_components/VehicleInput';
import { createOrUpdate } from '../createOrUpdate';
import Main from '../../_components/Main';
import { CardHeader } from '@/_components/Card';

export default async function Page() {
  
    return (
        <Main header={<CardHeader title='Create new vehicle' description='Enter details' ></CardHeader>}>
                <form action={createOrUpdate}>
                    <input type="hidden" name='id'  ></input>
                    <VehicleInput  ></VehicleInput>
                </form> 
        </Main> 
    )
}
