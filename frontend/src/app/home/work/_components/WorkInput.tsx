'use client'

import { useRouter } from 'next/navigation';
import { vehicleLabel } from '@/_lib/shared/vehicle';
import FormTextArea from '@/_components/FormTextArea';
import PrimaryButton from '@/_components/PrimaryButton';
import SecondaryButton from '@/_components/SecondaryButton';
import { IWorkData, IMechanic } from '../model';
import FormLabel from '@/_components/FormLabel';
import { ClientsCombobox, VehiclesCombobox } from '../../_components/SearchCombobox';
import { useState } from 'react';
import FormSwitch from '@/_components/FormSwitch';
import { Field, Label } from '@headlessui/react';
import { query } from '@/_lib/client/query-api';
import { IVehicleData } from '../../vehicles/model';
import FormInput from '@/_components/FormInput';
import Select from '@/_components/Select';
import clsx from 'clsx';
import WorkInputMechanics from './WorkInputMechanics';

const inputClass = 'mt-2 block w-full rounded-md bg-surface px-3 py-1.5 text-sm/6 text-gray-900 outline-1 -outline-offset-1 outline-gray-300 placeholder:text-gray-400 focus:outline-2 focus:-outline-offset-2 focus:outline-primary';

/** A text field of the walk-in customer (plain input: these can be required). */
function WalkInField({ name, label, required, type, placeholder, value, onChange, min, max }: {
    name: string, label: string, required?: boolean, type?: string, placeholder?: string,
    value?: string, onChange?: (value: string) => void, min?: number, max?: number,
}) {
    return (
        <div>
            <label htmlFor={name} className="block text-sm/6 font-medium text-gray-900">
                {label}{required && <span className="text-red-600"> *</span>}
            </label>
            <input id={name} name={name} type={type ?? 'text'} required={required} placeholder={placeholder} min={min} max={max}
                value={value} onChange={onChange ? (e) => onChange(e.currentTarget.value) : undefined}
                className={inputClass} />
        </div>
    )
}


export default function WorkInput({
    work,
    mechanics,
}: {
    work?: IWorkData | undefined,
    mechanics: IMechanic[]
}) {



    const router = useRouter()
   
    const [isOffer, setIsOffer] = useState(false);
    // walk-in customer: name, phone and vehicle typed in here instead of chosen (new work only)
    const [isWalkIn, setIsWalkIn] = useState(false);
    const [saveWalkIn, setSaveWalkIn] = useState(true);
    const [walkInPlate, setWalkInPlate] = useState('');
    const [walkInVin, setWalkInVin] = useState('');

    const [onlyClientVehicles, setOnlyClientVehicles] = useState(!work ? true : false);

    const [clientVehicles, setClientVehicles] = useState<IVehicleData[]>([]);
    const [selectedClientVehicleId, setSelectedClientVehicleId] = useState(work?.vehicleId ?? '');
    const [clientUndisclosed, setClientUndisclosed] = useState(!work ? false : !work.clientId);
    const [selectedClientId, setSelectedClientId] = useState(work?.clientId ?? '');
    // the list of the client's vehicles needs a client that has vehicles; otherwise all vehicles are searched
    const listClientVehicles = onlyClientVehicles && !clientUndisclosed && clientVehicles.length > 0;
    const populateClientVehicles = (clientId: string) => {
        query({
            url: 'vehicles/client/' + clientId,
            method: 'GET',
            onSuccess: (result: IVehicleData[]) => {
                if (result) {
                    setClientVehicles(result);
                    // one vehicle: take it; otherwise keep the choice when it is one of the client's vehicles
                    setSelectedClientVehicleId(current => result.length === 1 ? result[0].id : result.some(v => v.id === current) ? current : '');
                }
                else {
                    console.log(result);
                    return [];
                }
            },
            onFailure: ({ url, status, text }) => {
                console.log(url);
                console.log(text);
                console.log(status);
            }
        });
    }

    return (
        <>
            <div className="space-y-12 ">
                <div className="border-b  border-gray-900/10 pb-12">

                    <div className="grid grid grid-flow-row grid-cols-1  gap-4">
                        {!work && <div>
                            <FormLabel name='startWith' label='Start with'></FormLabel>
                            <Field className="flex mt-2 items-center">
                                <FormSwitch
                                    name='isOffer'
                                    checked={isOffer}
                                    onChange={(value) => {
                                        setIsOffer(value);
                                    }}></FormSwitch>

                                <Label as="span" className="ml-3 text-sm">
                                    Offer
                                </Label>
                            </Field>
                        </div>}

                        {!work && <div>
                            <Field className="flex items-center">
                                <FormSwitch
                                    name='isWalkIn'
                                    checked={isWalkIn}
                                    onChange={setIsWalkIn}></FormSwitch>
                                <Label as="span" className="ml-3 text-sm">
                                    <span className="font-medium text-gray-900">Walk-in customer</span>{' '}
                                    <span className="text-gray-500">— type in the customer and the vehicle</span>
                                </Label>
                            </Field>
                        </div>}

                        {isWalkIn && <div className="rounded-lg bg-gray-50 p-4 ring-1 ring-gray-900/5">
                            <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
                                <WalkInField name='walkInName' label='Customer name' required />
                                <WalkInField name='walkInPhone' label='Phone' type='tel' />
                                <WalkInField name='walkInManufacturer' label='Make' placeholder='e.g. Toyota' />
                                <WalkInField name='walkInModel' label='Model' placeholder='e.g. Corolla' />
                                <WalkInField name='walkInYear' label='Year' type='number' min={1900} max={new Date().getFullYear() + 1} />
                                <div className="hidden sm:block" />
                                <WalkInField name='walkInLicensePlate' label='License plate' required={walkInVin.trim() === ''}
                                    value={walkInPlate} onChange={setWalkInPlate} />
                                <WalkInField name='walkInVin' label='VIN' required={walkInPlate.trim() === ''}
                                    value={walkInVin} onChange={setWalkInVin} />
                            </div>
                            <p className="mt-2 text-xs text-gray-500">License plate or VIN is required.</p>
                            <Field className="mt-4 flex items-center">
                                <FormSwitch
                                    name='saveWalkIn'
                                    checked={saveWalkIn}
                                    onChange={setSaveWalkIn}></FormSwitch>
                                <Label as="span" className="ml-3 text-sm">
                                    Save to clients and vehicles
                                    <span className="block text-xs text-gray-500">
                                        {saveWalkIn ? 'A client and a vehicle are created (a vehicle with the same plate or VIN is reused).' : 'Kept on this work only.'}
                                    </span>
                                </Label>
                            </Field>
                        </div>}

                        {!isWalkIn && <div className=" ">
                            <FormLabel name='clientId' label='Client'>
                                <span className="ml-4 float-right text-gray-500">
                                    Undisclosed{' '}
                                    <FormSwitch
                                        name='clientUndisclosed'
                                        small={true}
                                        checked={clientUndisclosed}
                                        onChange={(value) => {
                                            setClientUndisclosed(value);
                                            setOnlyClientVehicles(!value);
                                        }}></FormSwitch>
                                </span>
                            </FormLabel>
                            {!clientUndisclosed &&
                                <ClientsCombobox name='clientId'
                                    onItemChange={(item) => {
                                        setSelectedClientId(item?.value ?? '');
                                        if (item?.value) populateClientVehicles(item.value);
                                        else setClientVehicles([]);
                                    }}
                                    defaultValue={{
                                        text: work?.clientName ?? '',
                                        value: work?.clientId ?? '',
                                    }}>
                                </ClientsCombobox>}

                        </div>}
                        <div className='  ' >
                            <FormLabel name={isWalkIn ? 'odo' : 'vehicleId'} label={isWalkIn ? 'Odometer' : 'Vehicle'}>
                                {!clientUndisclosed && !isWalkIn && <span className="ml-2 float-right text-gray-500">
                                    Search all vehicles{' '}
                                    <FormSwitch
                                        name='onlyClientVehicles'
                                        small={true}
                                        checked={!listClientVehicles}
                                        onChange={(value) => {
                                            setOnlyClientVehicles(!value);
                                            if (!value && selectedClientId && clientVehicles.length === 0) populateClientVehicles(selectedClientId);
                                        }}></FormSwitch>{' '}
                                </span>}

                            </FormLabel>
                            <div className='flex'>

                                {!isWalkIn && <div className="-mr-px grid grow grid-cols-1 focus-within:relative">
                                    <div className= {clsx(listClientVehicles&&"mt-2", " sm:col-span-2   grid grid-cols-1")}> 
                                        {listClientVehicles ?
                                            <Select
                                                name='vehicleId'
                                                value={selectedClientVehicleId}
                                                onChange={(e) => {
                                                    setSelectedClientVehicleId(e.currentTarget.value);
                                                }} >
                                                <option value="">— No vehicle —</option>
                                                {clientVehicles?.map((item, index) => {
                                                    return (<option key={index} value={item.id}>{vehicleLabel(item)}</option>)
                                                })}
                                            </Select> :
                                            <VehiclesCombobox name='vehicleId'

                                                defaultValue={{
                                                    text: vehicleLabel({ year: work?.vehicleYear, manufacturer: work?.vehicleManufacturer, model: work?.vehicleModel, licensePlate: work?.vehicleLicensePlate }),
                                                    value: work?.vehicleId ?? '',
                                                }}>
                                            </VehiclesCombobox>}
                                    </div>
                                </div>}
                                <div className={isWalkIn ? '' : 'ml-2  '}>
                                    <FormInput type='number' placeholder='Odometer value' name='odo' defaultValue={work?.odo ?? 0}></FormInput>
                                </div>
                            </div>

                        </div>
                       <WorkInputMechanics mechanics={mechanics} work={work}></WorkInputMechanics>
                        <div className=" ">
                            <FormTextArea name='about' rows={8} label='About' defaultValue={work?.notes}>
                            </FormTextArea>
                        </div>
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
