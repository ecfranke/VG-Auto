'use client'

import BaseDialog, { BaseDialogHandle } from "@/_components/BaseDialog";
import { IWorkData, paymentTypes } from "../../model";
import { useState } from "react";
import FormLabel from "@/_components/FormLabel";
import Select from "@/_components/Select";
import FormInput from "@/_components/FormInput";
import { issueInvoice } from "../../actions/issueInvoice";
import FormSwitch from "@/_components/FormSwitch";

export default function IssueInvoiceDialog({
    work,
    dialogRef
}: {
    work: IWorkData,
    dialogRef: React.RefObject<BaseDialogHandle | null>,
}) {
    const [selectedPaymentType, setSelectedPaymentType] = useState(2);
    const [dueDays, setDueDays] = useState(1);
    const [sendClientAnEmail,setSendClientAnEmail] = useState(!!work.clientEmail);
    const [clientEmail,setlientEmail] = useState(work.clientEmail??'');

    return (
        <BaseDialog ref={dialogRef}
            title="Complete the work and issue an Invoice"
            center={false}
            yesButtonText="OK"
            onConfirm={async () => {
                dialogRef.current?.loading(true);
                const result = issueInvoice({
                    workId: work.id,
                    dueDays: dueDays,
                    paymentType: selectedPaymentType,
                    sendClientEmail:sendClientAnEmail,
                    clientEmail:clientEmail, 
                })

                result.finally(() => {
                    dialogRef.current?.close();
                })
                await result;
            }}>
            <div className="space-y-12">
                <div className="border-b border-gray-900/10 pb-12">

                    <div className="mt-10 grid grid-cols-2 gap-x-2 gap-y-2 grid-cols-3">

                        <div  >
                            <FormLabel name="dueDays" label='Due days' ></FormLabel>
                            <div className=" col-span-2 grid grid-cols-1">
                                <FormInput name='dueDays' type="number" value={dueDays} onInputChange={(e) => setDueDays(+e.currentTarget.value)}></FormInput>
                            </div>
                        </div>
                        <div className="col-span-2" >
                            <FormLabel name='name' label='Payment type'></FormLabel>
                            <div className="mt-2 col-span-2 grid grid-cols-1">
                                <Select
                                    id="paymentType"
                                    value={selectedPaymentType}
                                    onChange={(e) => setSelectedPaymentType(+e.currentTarget.value)} >
                                    {Object.keys(paymentTypes).map((key, index) => {
                                        return <option key={key} id={key} value={index + 1}>{paymentTypes[key]}</option>
                                    }
                                    )}
                                </Select>
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
                        {sendClientAnEmail && <div className="col-span-full mt-4">
                            <FormLabel name='name' label='Client email'></FormLabel>
                            <div className="mt-2 col-span-2 grid grid-cols-1">
                                <FormInput
                                    name='clientEmail'

                                    onInputChange={(e) => {
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