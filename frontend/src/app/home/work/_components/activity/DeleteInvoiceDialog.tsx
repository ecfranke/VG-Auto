'use client'

import BaseDialog, { BaseDialogHandle } from "@/_components/BaseDialog"; 
import {   IWorkData  } from "../../model";  
import { deleteInvoice } from "../../actions/deleteInvoice";

export default function DeleteInvoiceDialog({
    work, 
    dialogRef
}:{
    work: IWorkData, 
    dialogRef: React.RefObject<BaseDialogHandle | null>, 
}){ 
     
    return (
        <BaseDialog ref={dialogRef}
                title="Delete an invoice"
                description="Are you sure you want to do this? Only last created invoice can be deleted, otherwise it will fail."
                center={false}
                yesButtonText="OK"
                onConfirm={async () => {

                    const result =  deleteInvoice(work.id) 
                     result.finally(()=>{
                        dialogRef.current?.close();
                     }) 
                     await result; 
                }}> 
                    <div className="space-y-12"> 
                    </div> 
            </BaseDialog>
    )
}