'use client'

import { useRouter } from 'next/navigation';
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


export default function WorkInput({
    work,
    mechanics,
}: {
    work?: IWorkData | undefined,
    mechanics: IMechanic[]
}) {



    const router = useRouter()
   
    const [isOffer, setIsOffer] = useState(false);

    const [onlyClientVehicles, setOnlyClientVehicles] = useState(!work ? true : false);

    const [clientVehicles, setClientVehicles] = useState<IVehicleData[]>([]);
    const [selectedClientVehicleId, setSelectedClientVehicleId] = useState(work?.vehicleId ?? '');
    const [clientUndisclosed, setClientUndisclosed] = useState(!work ? false : !work.clientId);
    const populateClientVehicles = (clientId: string) => {
        query({
            url: 'vehicles/client/' + clientId,
            method: 'GET',
            onSuccess: (result: IVehicleData[]) => {
                if (result) {
                    setClientVehicles(result);
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

                        <div className=" ">
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
                                        if (onlyClientVehicles && item) {
                                            populateClientVehicles(item.value);
                                        }
                                    }}
                                    defaultValue={{
                                        text: work?.clientName ?? '',
                                        value: work?.clientId ?? '',
                                    }}>
                                </ClientsCombobox>}

                        </div>
                        <div className='  ' >
                            <FormLabel name='vehicleId' label='Vehicle'>
                                {!clientUndisclosed && <span className="ml-2 float-right text-gray-500">
                                    Search all vehicles{' '}
                                    <FormSwitch
                                        name='onlyClientVehicles'
                                        small={true}
                                        checked={!onlyClientVehicles}
                                        onChange={(value) => {
                                            setOnlyClientVehicles(!value);
                                        }}></FormSwitch>{' '}
                                </span>}

                            </FormLabel>
                            <div className='flex'>

                                <div className="-mr-px grid grow grid-cols-1 focus-within:relative">
                                    <div className= {clsx(onlyClientVehicles&&"mt-2", " sm:col-span-2   grid grid-cols-1")}> 
                                        {onlyClientVehicles ?
                                            <Select
                                                name='vehicleId'
                                                value={selectedClientVehicleId}
                                                onChange={(e) => {
                                                    setSelectedClientVehicleId(e.currentTarget.value);
                                                }} >
                                                {clientVehicles?.map((item, index) => {
                                                    return (<option key={index} value={item.id}>{[item?.producer, item?.model].filter(x => x).join(' ') + (!item?.regNr ? '' : ` (${item.regNr})`)}</option>)
                                                })}
                                            </Select> :
                                            <VehiclesCombobox name='vehicleId'

                                                defaultValue={{
                                                    text: [work?.vehicleProducer, work?.vehicleModel].filter(x => x).join(' ') + (!work?.vehicleRegNr ? '' : `(${work?.vehicleRegNr})`),
                                                    value: work?.vehicleId ?? '',
                                                }}>
                                            </VehiclesCombobox>}
                                    </div>
                                </div>
                                <div className='ml-2  '>
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
