'use client'

import FormInput from '@/_components/FormInput';
import { useRouter } from 'next/navigation';
import FormTextArea from '@/_components/FormTextArea';
import PrimaryButton from '@/_components/PrimaryButton';
import SecondaryButton from '@/_components/SecondaryButton'; 
import { IVehicleData } from '../model'; 
import FormLabel from '@/_components/FormLabel';
import TypeAheadCombobox from '../../_components/TypeAheadCombobox';
import  { ClientsCombobox } from '../../_components/SearchCombobox';
import data from './car_brands.json'; 
import { useState } from 'react';

interface ICarProducer
{
    name:string
}

export default function VehicleInput({
    vehicle
}: {
    vehicle?: IVehicleData | undefined
}) {



    const router = useRouter()
     
    const [producer,setProducer] = useState<ICarProducer |null>(!vehicle?null:{name:vehicle.producer})
    return (
        <>
            <div className="space-y-12">
                <div className="border-b border-gray-900/10 pb-12">
                    
                    <div className="grid grid-cols-1 gap-x-6 gap-y-8 sm:grid-cols-6">
                    <div className="sm:col-span-2">
                        <FormLabel name='producer' label='Car make'></FormLabel>
                        <TypeAheadCombobox 
                          name="producer" 
                          defaultValue={producer} 
                          displayFormatter={(item)=>!item?'':item?.name} 
                          optionFormatter={(item)=>!item?'':item?.name} 
                          placeholder="Enter car make" 
                          onItemChange={(item)=>{
                              
                            setProducer(item);
                          }} 
                          onSearch={(e,dataTarget)=>{
                             const inputValue = e.currentTarget.value;
                             if(inputValue){
                                //dataTarget()
                                const makesFound = data.filter((make)=>{
                                    return make.name.toLowerCase().startsWith(inputValue.toLowerCase());
                                }) as ICarProducer[];
                                dataTarget(makesFound);
                             }
                             
                          }}
                          ></TypeAheadCombobox> 
                       </div> 
                        <div className="sm:col-span-2">  <FormInput name='model' defaultValue={vehicle?.model} label='Vehicle model'></FormInput></div>
                        <div className="sm:col-span-2">  <FormInput name='vin' defaultValue={vehicle?.vin} label='VIN Code'></FormInput></div>
                        <div className="sm:col-span-2">  <FormInput name='regNr' defaultValue={vehicle?.regNr} label='Registration nr'></FormInput></div>
                        <div className="sm:col-span-2">  <FormInput name='odo' defaultValue={vehicle?.odo} label='Odometer'></FormInput> </div>
                        <div className="col-span-full">
                            <FormLabel name='ownerId' label='Owner'></FormLabel>
                            <ClientsCombobox  
                               name='ownerId'
                                defaultValue={{
                                    text: vehicle?.ownerName??'',
                                    value: vehicle?.ownerId??'',
                                }}>
                            </ClientsCombobox>
                        </div>

                    </div>
                </div>
            </div>
            <div className="border-b border-gray-900/10 pb-12">
                <div className="mt-4 grid grid-cols-1 gap-x-6 gap-y-8 sm:grid-cols-6">

                    <div className="col-span-full">
                        <FormTextArea name='about' label='About' defaultValue={vehicle?.description}>
                        </FormTextArea>
                    </div>
                </div>
            </div>
            <div className="mt-6 flex items-center justify-end gap-x-6">
                <SecondaryButton onClick={() => router.back()}>Cancel</SecondaryButton>
                <PrimaryButton onClick={() => { }}>Save</PrimaryButton>
            </div>
        </>
    )
}
