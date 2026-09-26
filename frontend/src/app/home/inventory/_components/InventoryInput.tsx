'use client'

import FormInput from '@/_components/FormInput';
import FormTextArea from '@/_components/FormTextArea';
import PrimaryButton from '@/_components/PrimaryButton';
import SecondaryButton from '@/_components/SecondaryButton'; 
import { ILocation, ISparepartData } from '../model';
import FormLabel from '@/_components/FormLabel'; 
import { useRouter } from 'next/navigation'; 
import React from 'react';import NamedLocation from './NamedLocation';
 ; 

 
export default function InventoryInput({
    allLocations,
    sparepart
}: {
    allLocations: ILocation[] ,
    sparepart?: ISparepartData | undefined
}) {

    const router = useRouter();
    
     
    return (
        <>   
            <div className="space-y-12">
                <div className="border-b border-gray-900/10 pb-12">
                   
                    <div className=" grid grid-cols-1 gap-x-6 gap-y-8 sm:grid-cols-6">
                        <div className="sm:col-span-2"> <FormInput name='code' defaultValue={sparepart?.code} label='Product code'></FormInput></div>
                        <div className="sm:col-span-2"> <FormInput name='name' defaultValue={sparepart?.name} label='Product name'></FormInput></div> 
                        <div className="sm:col-span-2"> <FormInput name='price' type='number' step='any'   defaultValue={sparepart?.price} label='Price'></FormInput></div>
                        <div className="sm:col-span-2"> <FormInput name='quantity' type='number' step='any'  defaultValue={sparepart?.quantity} label='Quantity'></FormInput></div>
                        <div className="lg:col-span-4 sm:col-span-full">
                            <FormLabel name='location' label='Location'></FormLabel>
                            <NamedLocation sparepartLocationId={sparepart?.storageId} allLocations={allLocations}></NamedLocation>
                        </div>
                    </div> 
                </div>
            </div>
            <div className="border-b border-gray-900/10 pb-12">
                <div className="mt-4 grid grid-cols-1 gap-x-6 gap-y-8 sm:grid-cols-6">

                    <div className="col-span-full">
                        <FormTextArea name='about' label='About' defaultValue={sparepart?.description}>
                        </FormTextArea>
                    </div>
                </div>
            </div>
            <div className="mt-6 flex items-center justify-end gap-x-6">
                <SecondaryButton onClick={() => router.back()}>Cancel</SecondaryButton>
                <PrimaryButton   onClick={() => { }}>Save</PrimaryButton>
            </div>
        </>
    )
}
