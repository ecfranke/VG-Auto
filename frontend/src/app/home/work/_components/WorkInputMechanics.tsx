'use client'

import { ChevronDownIcon, UserCircleIcon } from '@heroicons/react/20/solid';
import FormList from '@/_components/FormList';
import { useState } from 'react';
import { IMechanic, IWorkData } from '../model';
import FormLabel from '@/_components/FormLabel';
import React from 'react';
import ConfirmDialog, { ConfirmDialogHandle } from '@/_components/ConfirmDialog';
import BaseDialog, { BaseDialogHandle } from '@/_components/BaseDialog';
import ButtonGroup, { IButtonOption } from '@/_components/ButtonGroup';
import { addEmployee, removeEmployee } from '../actions/addOrRemoveEmployee';
import FormInput from '@/_components/FormInput';

export default function WorkInputMechanics({
    work,
    mechanics,
}: {
    work?: IWorkData | undefined,
    mechanics: IMechanic[]
}) {
    const [selectedMechanicId, setSelectedMechanicId] = useState('');
    const [selectedMechanics, setSelectedMechanics] = useState<IMechanic[]>(work?.mechanics ?? [])
    const [newMechanic, setNewMechanic] = useState('');
    const newLocationDialogRef = React.useRef<BaseDialogHandle>(null);
    const confirmRemoveLocationRef = React.useRef<ConfirmDialogHandle>(null);
    const [allMechanics,setAllMechanics] = useState(mechanics);

    const addOrRemoveOption = [
        {
            name: 'New',
            onClick: () => {
                newLocationDialogRef?.current?.open();
            },
            inMenu: false
        },
        {
            name: 'Delete mechanic',
            onClick: () => {
                if (!selectedMechanicId) return;
               
                const itemToRemove = allMechanics.find(x => x.id.toString() == selectedMechanicId)?.name;
                if(!itemToRemove) return;
                confirmRemoveLocationRef?.current?.open({
                    title: "About to remove '" + itemToRemove + "'",
                    description: "Are you sure you want to remove this mechanic? Make sure mechanic is not assigned to any work, otherwise it cannot be removed.",
                    confirmObj: selectedMechanicId
                });
            },
            inMenu: true,
            redText: true
        }
    ] as IButtonOption[]

    return (
        <div className='   ' >
            <FormLabel name='mechanics' label='Mechanics'></FormLabel>

            <div className="mt-2 mb-2  flex">
                <div className="-mr-px grid grow grid-cols-1 focus-within:relative">
                    <div className="  sm:col-span-2 grid grid-cols-1">
                        <select
                            value={selectedMechanicId}
                            onChange={(e) => {
                                setSelectedMechanicId(e.currentTarget.value);
                            }}
                            className="col-start-1 row-start-1 w-full appearance-none rounded-l-md bg-white py-1.5 pr-8 pl-3 text-base text-gray-900 outline-1 -outline-offset-1 outline-gray-300 focus:outline-2 focus:-outline-offset-2 focus:outline-indigo-600 text-sm/6"
                        >
                            <option value={''}></option>
                            {mechanics?.map((item, index) => {
                                return (<option key={index} value={item.id}>{item.name}</option>)
                            })}
                        </select>
                        <ChevronDownIcon
                            aria-hidden="true"
                            className="pointer-events-none col-start-1 row-start-1 mr-2 size-5 self-center justify-self-end text-gray-500 sm:size-4"
                        />
                    </div>
                </div>
                <button
                    type="button"
                    onClick={() => {
                        const newMechanic = allMechanics.find(x => x.id == selectedMechanicId);
                        if (newMechanic) {
                            const newSet = [...selectedMechanics, newMechanic]
                            setSelectedMechanics(newSet)
                        }
                    }}
                    className="flex shrink-0 items-center gap-x-1.5 rounded-r-md bg-indigo-600 px-3 py-2 text-sm font-semibold text-white outline-1 -outline-offset-1 outline-indigo-300 hover:bg-indigo-500 focus:relative focus:outline-2 focus:-outline-offset-2 focus:outline-indigo-600"
                >
                    Add
                </button>
                <ButtonGroup options={addOrRemoveOption}></ButtonGroup>
            </div>

            <ConfirmDialog onConfirm={async (targetItem: string) => {

                await removeEmployee(targetItem);
                allMechanics.splice(allMechanics.findIndex(x => x.id == targetItem), 1);
                setAllMechanics([...allMechanics]);
            }} ref={confirmRemoveLocationRef} ></ConfirmDialog>

            <BaseDialog ref={newLocationDialogRef} yesButtonText="Save" title='Add new mechanic'
                onConfirm={async () => {

                    newLocationDialogRef.current?.loading(true);
                    const addpromise = addEmployee(newMechanic);
                    addpromise.finally(() => {
                        newLocationDialogRef.current?.close();
                    })
                    const newMechanicId = await addpromise;
                    const newMechanicItem = {
                        id: newMechanicId,
                        name: newMechanic
                    };
                    setAllMechanics([...allMechanics, newMechanicItem])
                    setSelectedMechanicId(newMechanicId);
                }}>
                <div className="py-8 px-8 text-center">
                    <FormInput
                        name='item'
                        defaultValue={newMechanic}

                        placeholder='Enter new mechanics name'
                        onInputChange={(e) => setNewMechanic(e.currentTarget.value)}
                    ></FormInput>
                </div>
            </BaseDialog>
            <FormList
                items={selectedMechanics}
                renderItem={(item) => {
                    return (
                        <> <div className="flex w-0 flex-1 items-center">
                            <UserCircleIcon aria-hidden="true" className="size-5 shrink-0 text-gray-400" />
                            <div className="ml-4 flex min-w-0 flex-1 gap-2">
                                <span className="truncate font-medium">{item.name}</span>
                            </div>
                        </div>
                            <input type="hidden" name='mechanics' defaultValue={item.id} ></input>
                            <div className="ml-4 flex shrink-0 space-x-4">

                                <button type="button"
                                    onClick={() => {
                                        selectedMechanics.splice(selectedMechanics.indexOf(item), 1);
                                        setSelectedMechanics([...selectedMechanics]);
                                    }}
                                    className="rounded-md bg-white font-medium text-gray-900 hover:text-gray-800">
                                    Remove
                                </button>
                            </div>
                        </>
                    )

                }}>
            </FormList>

        </div>
    )
}