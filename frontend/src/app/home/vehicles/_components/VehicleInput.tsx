'use client'

import FormInput from '@/_components/FormInput';
import { useRouter } from 'next/navigation';
import FormTextArea from '@/_components/FormTextArea';
import PrimaryButton from '@/_components/PrimaryButton';
import SecondaryButton from '@/_components/SecondaryButton'; 
import { IVehicleData } from '../model'; 
import FormLabel from '@/_components/FormLabel';
import SuggestCombobox from '../../_components/SuggestCombobox';
import  { ClientsCombobox } from '../../_components/SearchCombobox';
import { MANUFACTURERS, modelsOf } from '@/_lib/shared/vehicleModels';
import { useState } from 'react';

export default function VehicleInput({
    vehicle
}: {
    vehicle?: IVehicleData | undefined
}) {



    const router = useRouter()
     
    // the manufacturer first: its models are then suggested; both can also be typed in
    const [manufacturer, setManufacturer] = useState(vehicle?.manufacturer ?? '')
    const [model, setModel] = useState(vehicle?.model ?? '')
    const models = modelsOf(manufacturer)
    return (
        <>
            <div className="space-y-12">
                <div className="border-b border-gray-900/10 pb-12">
                    
                    <div className="grid grid-cols-1 gap-x-6 gap-y-8 sm:grid-cols-6">
                    <div className="sm:col-span-2">
                        <FormLabel name='manufacturer' label='Manufacturer'></FormLabel>
                        <SuggestCombobox id='manufacturer' name='manufacturer' value={manufacturer} options={MANUFACTURERS}
                          placeholder='e.g. Honda' onChange={setManufacturer} />
                    </div>
                    <div className="sm:col-span-2">
                        <FormLabel name='model' label='Model'></FormLabel>
                        <SuggestCombobox id='model' name='model' value={model} options={models} onChange={setModel}
                          placeholder={manufacturer ? 'e.g. ' + (models[0] ?? 'model') : 'Choose the manufacturer first'}
                          emptyHint={manufacturer ? 'Type the model' : 'Choose the manufacturer first'} />
                    </div>
                        <div className="sm:col-span-2">  <FormInput name='trim' defaultValue={vehicle?.trim ?? undefined} label='Trim' placeholder='e.g. LX, Touring'></FormInput></div>
                        <div className="sm:col-span-2">  <FormInput name='year' type='number' defaultValue={vehicle?.year ?? undefined} label='Year' placeholder='e.g. 2019'></FormInput></div>
                        <div className="sm:col-span-2">  <FormInput name='vin' defaultValue={vehicle?.vin} label='VIN Code'></FormInput></div>
                        <div className="sm:col-span-2">  <FormInput name='licensePlate' defaultValue={vehicle?.licensePlate} label='License plate'></FormInput></div>
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
