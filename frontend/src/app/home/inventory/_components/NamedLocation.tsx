'use client'

import BaseDialog, { BaseDialogHandle } from "@/_components/BaseDialog";
import ConfirmDialog, { ConfirmDialogHandle } from "@/_components/ConfirmDialog";
import React, { useState } from "react";
import { ILocation } from "../model";
import Select from "@/_components/Select";
import { addLocation, removeLocation } from "../addOrRemoveLocation";
import FormInput from "@/_components/FormInput";
import ButtonGroup, { IButtonOption } from "@/_components/ButtonGroup";

export default function NamedLocation({
    allLocations ,
    sparepartLocationId
}: {
    allLocations: ILocation[] ,
    sparepartLocationId?: string 
}){

    const  newLocationDialogRef =  React.useRef<BaseDialogHandle>(null);
    const confirmRemoveLocationRef =  React.useRef<ConfirmDialogHandle>(null);

    const [locations, setLocations] = useState<ILocation[]>(allLocations); 
    const[newLocation,setNewLocation]=useState('');
  
    const [selectedLocationId, setSelectedLocationId] = useState(sparepartLocationId??'');
 
    const addOrRemoveOption = [
        {
            name: 'New',
            onClick:() => {
                newLocationDialogRef?.current?.open();
           },
           inMenu:false
        },
        {
            name:'Delete location',
            onClick:() => {
                if(!selectedLocationId) return; 
                 const itemToRemove =   locations.find(x=>x.id.toString() == selectedLocationId)?.name;
                  confirmRemoveLocationRef?.current?.open({
                    title:"About to remove '"+itemToRemove+"'",
                      description:"Are you sure you want to remove the location? Make sure no spare parts are using this location, otherwise it cannot be removed.",
                    confirmObj: selectedLocationId
                 });
            },
            inMenu:true,
            redText:true
        }
    ] as IButtonOption[]
    
    return (
        <div className="mt-2 grid grid-cols-3">
            <div className="sm:col-span-2 grid grid-cols-1">
                <Select
                    id="storage"
                    name="storage"
                    value={selectedLocationId}
                    onChange={(e) => {
                        setSelectedLocationId(e.currentTarget.value);
                    }}>
                        <option value="">-</option>
                    {locations?.map((item, index) => {
                        return (<option key={index} value={item.id}>{item.name}</option>)
                    })}
                </Select>
            </div>
            <div className="ml-5 grid grid-cols-1">
                 <ButtonGroup options={addOrRemoveOption}></ButtonGroup> 

            </div>
            <ConfirmDialog onConfirm={async (targetItem:string)=>{

                await removeLocation(targetItem); 
                locations.splice(locations.findIndex(x=>x.id == targetItem),1);
                setLocations([...locations]); 
           }}  ref={confirmRemoveLocationRef} ></ConfirmDialog>

            <BaseDialog ref={newLocationDialogRef} yesButtonText="Save" title='Add new named location' 
                onConfirm={async () => { 
                     
                    newLocationDialogRef.current?.loading(true);
                    const addpromise = addLocation(newLocation);
                    addpromise.finally(()=>{
                        newLocationDialogRef.current?.close();
                    })
                    const newLocatoinId = await addpromise;
                    const newLocationItem= {
                        id: newLocatoinId,
                        name: newLocation
                    };
                    setLocations([...locations,newLocationItem])
                    setSelectedLocationId(newLocatoinId.toString());
                }}>
                <div className="py-8 px-8 text-center"> 
                    <FormInput
                        name='item'
                        defaultValue={newLocation}
                        
                        placeholder='Enter new location name'
                        onInputChange={(e) => setNewLocation(e.currentTarget.value)}
                    ></FormInput>
                </div>
            </BaseDialog>
        </div>
       
    )
}