 
import PrimaryButton from '@/_components/PrimaryButton';
import SecondaryButton from '@/_components/SecondaryButton';
import { Dialog, DialogBackdrop, DialogPanel, DialogTitle } from '@headlessui/react'
import React from 'react';
import { useState,  useImperativeHandle  } from 'react';
import { ExclamationCircleIcon } from '@heroicons/react/24/outline'

 export interface IOnConfirm 
 {
    (confirmObj:any ):void // eslint-disable-line @typescript-eslint/no-explicit-any
 }
 
export type ConfirmDialogHandle  = {
    open: ({
        title,
        description,
        confirmObj, 
    }:{
        title:string,
        description?:string |undefined ,
        confirmObj: any,  // eslint-disable-line @typescript-eslint/no-explicit-any
    }) => void;
  };
  
 type Props  = {
    onConfirm: IOnConfirm 
 };

 const ConfirmDialog = React.forwardRef<ConfirmDialogHandle , Props >((props, ref) =>{

    const { onConfirm} =props;  
    const [isDialogOpen, setIsDialogOpen] = useState(false);
    const [title, setTitle] = useState('');
    const [description, setDescription] = useState('');
    const [confirmObjTarget, setConfirmObjTarget] = useState();
     
    const confirm =()=>{
        setIsDialogOpen(false);
        onConfirm(confirmObjTarget);
    }

    useImperativeHandle(ref, () => {
        return {
          open({
            title,
            description,
            confirmObj, 
        }) {
            setTitle(title);
            if(description)setDescription(description);
            setIsDialogOpen(true); 
            setConfirmObjTarget(confirmObj);
          }, 
        };
      }, []);

   return (
    <Dialog open={isDialogOpen} onClose={setIsDialogOpen} className="relative z-10">
    <DialogBackdrop
        transition
        className="fixed inset-0 bg-gray-500/75 transition-opacity data-closed:opacity-0 data-enter:duration-300 data-enter:ease-out data-leave:duration-200 data-leave:ease-in"
    />

    <div className="fixed inset-0 z-10 w-screen overflow-y-auto">
        <div className="flex min-h-full items-end justify-center p-4 text-center sm:items-center sm:p-0">
            <DialogPanel
                transition
                className="relative transform overflow-hidden rounded-lg bg-white px-4 pt-5 pb-4 text-left shadow-xl transition-all data-closed:translate-y-4 data-closed:opacity-0 data-enter:duration-300 data-enter:ease-out data-leave:duration-200 data-leave:ease-in sm:my-8 sm:w-full sm:max-w-lg sm:p-6 data-closed:sm:translate-y-0 data-closed:sm:scale-95"
            >
                 <div>
               <div className="mx-auto flex size-12 items-center justify-center rounded-full bg-green-100">
                <ExclamationCircleIcon aria-hidden="true" className="size-6 text-green-600" />
                </div>
                <div className="mt-3 text-center sm:mt-5">
                    <DialogTitle as="h3" className="text-base font-semibold text-gray-900">
                    {title}
                    </DialogTitle>
                   {description&& <div className="mt-2">
                    <p className="text-sm text-gray-500">
                        {description}
                    </p>
                    </div>}
                </div>
            </div>
                <div className="mt-6    grid  grid-flow-row-dense  grid-cols-2  gap-3">
                    
                    <SecondaryButton
                      onClick={() => setIsDialogOpen(false)}
                    >Cancel</SecondaryButton>
                     <PrimaryButton
                     className="inline-flex w-full justify-center" 
                     onClick={() => { 
                        confirm();
                     }}
                    >Yes
                    </PrimaryButton>
                </div>
            </DialogPanel>
        </div>
    </div>
</Dialog>
   )
});
ConfirmDialog.displayName = "Confirm dialog";

export default ConfirmDialog;
 