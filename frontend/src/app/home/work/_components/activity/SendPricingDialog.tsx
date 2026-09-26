'use client'

import BaseDialog, { BaseDialogHandle } from "@/_components/BaseDialog";
import { IWorkData } from "../../model";
import { useState } from "react";
import FormLabel from "@/_components/FormLabel"; 
import FormInput from "@/_components/FormInput";import { sendPricing } from "../../actions/sendPricing";
 ;

export default function SendPricingDialog({
    work,
    offerId, 
    dialogRef
}: {
    work: IWorkData,
    offerId?: string, 
    dialogRef: React.RefObject<BaseDialogHandle | null>,
}) { 
    const [clientEmail,setlientEmail] = useState(work.clientEmail??'');

    const forInvoice = !offerId;
    const title = forInvoice? 'Send invoice': 'Send offer';
    return (
        <BaseDialog ref={dialogRef}
            title={title}
            center={false} 
            yesButtonText="OK"
            onConfirm={async () => {
                dialogRef.current?.loading(true);

                const result =  sendPricing({
                    workId: work.id, 
                    offerId, 
                    clientEmail:clientEmail, 
                })

                result.finally(() => {
                    dialogRef.current?.close();
                })
                await result;
            }}>
            <div className="space-y-12  ">
                <div className="border-b mt-4  border-gray-900/10 pb-12">
                      <FormLabel name='name' label='Client email'></FormLabel>
                            <div className="mt-2   ">
                                <FormInput
                                    name='clientEmail' 
                                    onInputChange={(e) => {
                                        setlientEmail(e.currentTarget.value);
                                    }}
                                    value={clientEmail}
                                >
                                </FormInput>
                            </div>
                </div>
            </div>
        </BaseDialog>
    )
}