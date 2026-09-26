'use client'

import BaseDialog, { BaseDialogHandle } from "@/_components/BaseDialog";
import FormInput from "@/_components/FormInput"; 
import { IActivities,   IWorkData } from "../../model";
import {  useState } from "react";
import { issueAnOffer } from "../../actions/issueAnOffer";
import FormLabel from "@/_components/FormLabel";
import FormSwitch from "@/_components/FormSwitch";

export default function IssueOfferDialog({
    work,
    activities, 
    dialogRef
}:{
    work: IWorkData,
    activities: IActivities,  
    dialogRef: React.RefObject<BaseDialogHandle | null>, 
}){
    const [sendClientAnEmail,setSendClientAnEmail] = useState(!!work.clientEmail);
    const [clientEmail,setlientEmail] = useState(work.clientEmail??'');
    const [isVehicelLinesOnPricing, setIsVehicelLinesOnPricing] = useState(activities.current.isVehicleLinesOnPricing);
   
    const activityName = activities.items.find(x => x.id === activities.current.id)?.name;
     
    return (
        <BaseDialog ref={dialogRef}
                title="Issue an offer"
                center={false}
                yesButtonText="OK"
                onConfirm={async () => {

                   const offer = activities.items.find(x => x.id === activities.current.id);
                   if(offer){ 
                    dialogRef.current?.loading(true);
                    const result =   issueAnOffer({
                        workId: work.id,
                        activityId: activities.current.id,
                        offerNumber: +offer.number,
                        showVehicleOnPricing:isVehicelLinesOnPricing,
                        sendClientEmail:sendClientAnEmail,
                        clientEmail:clientEmail, 
                       })
                     
                     result.finally(()=>{
                        dialogRef.current?.close();
                     })

                     await result;

                   }  
                }}> 
                    <div className="space-y-12">
                        <div className="border-b border-gray-900/10 pb-12"> 
                            <div className="mt-10 grid grid-cols-1 gap-x-2 gap-y-2 grid-cols-6"> 
                                <div className="col-span-full">
                                <div className="flex items-end">
                                        <div className="flex-auto  ">
                                        <FormLabel name="isVehicelLinesOnPricing" label={'Show vehicle information on ' + (activityName == 'repairjob' ? 'invoice' : 'offer')} ></FormLabel>
                                        </div>
                                        <div className="mt-2  ">
                                        <FormSwitch name='isVehicelLinesOnPricing' checked={isVehicelLinesOnPricing} onChange={(value) => setIsVehicelLinesOnPricing(value)}></FormSwitch>
                                        </div> 
                                    </div> 
                                </div>
                                <div className="col-span-full">
                                <div className="flex items-end">
                                        <div className="flex-auto  ">
                                        <FormLabel name="sendClientAnEmail" label='Send client an email' ></FormLabel>
                                        </div>
                                        <div className="mt-2  ">
                                        <FormSwitch name='sendClientAnEmail' checked={sendClientAnEmail} onChange={(value) => {
                                            
                                            setSendClientAnEmail(value);
                                        }}></FormSwitch>
                                        </div> 
                                    </div> 
                                </div>
                                {sendClientAnEmail&&<div className="col-span-full mt-4">
                                    <FormLabel name='name' label='Client email'></FormLabel> 
                                    <div className="mt-2 col-span-2 grid grid-cols-1">
                                        <FormInput
                                            name='clientEmail'
                                            
                                            onInputChange={(e)=>{
                                                setlientEmail(e.currentTarget.value);
                                            }}
                                            value={clientEmail}
                                            >
                                        </FormInput>
                                    </div>
                                </div>}
                                
                            </div>
                        </div>
                    </div> 
            </BaseDialog>
    )
}