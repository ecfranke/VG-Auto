'use client'

import BaseDialog, { BaseDialogHandle } from "@/_components/BaseDialog";
import { IActivities, IWorkData } from "../../model";
import { useState } from "react";
import FormLabel from "@/_components/FormLabel";
import FormSwitch from "@/_components/FormSwitch";
import { offerAccepted } from "../../actions/offerAccepted";
import Select from "@/_components/Select";
import FormTextArea from "@/_components/FormTextArea";
import { getActivityDisplayName } from "./getActivityDisplayName";

export default function OfferAcceptedDialog({
    work,
    activities,
    dialogRef
}: {
    work: IWorkData,
    activities: IActivities,
    dialogRef: React.RefObject<BaseDialogHandle | null>,
}) {


    const repairJobs = activities.items.filter(x => x.name == 'repairjob');
    const [startNewRepairJob, setStartNewRepairJob] = useState(repairJobs.length === 0);
    const defaultSelected = repairJobs.length > 0 ? repairJobs[0].number : '';
    const [selectedRepairJobNumber, setSelectedRepairJobNumber] = useState(defaultSelected);
    const [notes, setNotes] = useState('');


    return (
        <BaseDialog ref={dialogRef}
            title="Offer was accepted by client"

            center={false}
            yesButtonText="OK"
            onConfirm={async () => {

                const offer = activities.items.find(x => x.id === activities.current.id);
                if (offer) {
                    dialogRef.current?.loading(true);
                    const result = offerAccepted({
                        workId: work.id,
                        targetJobNumber: startNewRepairJob ? '' : selectedRepairJobNumber,
                        offerNumber: +offer.number,
                        notes
                    })

                    result.finally(() => {
                        dialogRef.current?.close();
                    })

                    await result;

                }
            }}>
            <div className="space-y-12">
                <div className="border-b border-gray-900/10 pb-12">
                    <div className=" pt-8">
                        <p className="text-sm   text-gray-500">
                            Create a new job or update existing based on what offer contained.
                        </p>
                    </div>
                    <div className="mt-10 grid grid-cols-1 gap-x-6 gap-y-8 sm:grid-cols-6">

                        {repairJobs.length > 0 && <>  <div className="sm:col-span-full">
                            <div className="sm:flex sm:items-end">
                                <div className="sm:flex-auto  ">
                                    <FormLabel name="startNewRepairJob" label='Start a new repair job' ></FormLabel>
                                </div>
                                <div className="mt-2  ">
                                    <FormSwitch name='startNewRepairJob' checked={startNewRepairJob} onChange={(value) => setStartNewRepairJob(value)}></FormSwitch>
                                </div>
                            </div>
                        </div>
                            {!startNewRepairJob && <div className="sm:col-span-full">
                                <FormLabel name='name' label='Repair job to add services/products'></FormLabel>
                                <div className="mt-6 sm:col-span-2 grid grid-cols-1">
                                    <Select
                                        id="selectedRepairJob"
                                        value={selectedRepairJobNumber}
                                        onChange={(e) => setSelectedRepairJobNumber(e.currentTarget.value)} >
                                        {repairJobs.map(job => {
                                            return <option key={job.id} id={job.number} value={job.number}>{getActivityDisplayName(job.name, job.number)}</option>
                                        }
                                        )}
                                    </Select>
                                </div>
                            </div>}</>} 

                        {startNewRepairJob && <div className="sm:col-span-full">
                            <FormLabel name="notes" label={'Notes to add to new job'} ></FormLabel>
                            <div className="mt-2  ">
                                <FormTextArea
                                    name="notes"
                                    value={notes}
                                    onInputChange={(e) => setNotes(e.currentTarget.value)}
                                ></FormTextArea>
                            </div>
                        </div>
                        }
                    </div>

                </div>
            </div>
        </BaseDialog>
    )
}