'use client'


import BaseDialog, { BaseDialogHandle } from "@/_components/BaseDialog";
import FormInput from "@/_components/FormInput";
import { DataItemRowHandle } from "../editabletable/DataIItemRow";
import { IProduct } from "../../model";
import { useState } from "react";
import FormLabel from "@/_components/FormLabel";

export default function ApplyDiscountsDialog({
    dialogRef,
    tableRef
}:{
    dialogRef: React.RefObject<BaseDialogHandle | null>,
    tableRef: React.RefObject<DataItemRowHandle<IProduct>[] | null[]>
}){
    const [discount, setDiscount] = useState<number | undefined>();

    return (
        <BaseDialog ref={dialogRef}
        yesButtonText="Apply"
        center={false} 
        title="Apply Discount" 
        onConfirm={() => {
            if (discount)
                tableRef.current.forEach(r => {
                    r?.applyDiscount(discount); 
                })
           dialogRef.current?.close();
        }}>
         <div className="space-y-12  ">
         <div className="border-b mt-4  border-gray-900/10 pb-12">
          <FormLabel name='name' label='Percent'></FormLabel>
         <FormInput
                name='item'
                type="number" 
                onInputChange={(e) => setDiscount(+e.currentTarget.value)}
            ></FormInput>
         </div>
         </div>
         
    </BaseDialog>
    )
}