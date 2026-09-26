import PrimaryButton from '@/_components/PrimaryButton';
import SecondaryButton from '@/_components/SecondaryButton';
import { Dialog, DialogBackdrop, DialogPanel, DialogTitle } from '@headlessui/react'
import clsx from 'clsx';
import React from 'react';
import { useState, useImperativeHandle } from 'react';
import Spinner from './Spinner';

export interface IOnConfirm {
  (): void
}

export type BaseDialogHandle = {
  open: () => void;
  close: () => void;
  loading: (value: boolean) => void;
};

type Props = {
  title?: string | undefined,
  description?: string | undefined,
  noButtonText?: string | undefined,
  yesButtonText?: string | undefined
  onConfirm: IOnConfirm,
  center?: boolean | undefined,
  children: React.ReactNode
};

const BaseDialog = React.forwardRef<BaseDialogHandle, Props>((props, ref) => {

  const { title, description, center = true, noButtonText = 'Cancel', yesButtonText = 'Yes', onConfirm, children } = props;
  const [isDialogOpen, setIsDialogOpen] = useState(false);
  const [isLoading, setIsLoading] = useState(false);

  const confirm = () => {

    onConfirm();
  }

  useImperativeHandle(ref, () => {
    return {
      close() {
        setIsDialogOpen(false);
        setIsLoading(false);
      },
      open() {
        setIsDialogOpen(true);
      },

      loading(value: boolean) {
        setIsLoading(value);
      }
    };
  }, []);

  return (
    <Dialog open={isDialogOpen} onClose={setIsDialogOpen} className="relative z-10">
      <DialogBackdrop
        transition
        className="fixed inset-0 bg-gray-500/75 transition-opacity data-closed:opacity-0 data-enter:duration-300 data-enter:ease-out data-leave:duration-200 data-leave:ease-in"
      />

      <div className="fixed inset-0 z-10 w-screen overflow-y-auto">
        <div className={clsx("flex min-h-full justify-center text-center sm:items-center p-4 items-end  sm:p-0")}>
          <DialogPanel
            transition
            className="relative auto grow transform overflow-hidden rounded-lg bg-white px-4 pt-5 pb-4 text-left shadow-xl transition-all data-closed:translate-y-4 data-closed:opacity-0 data-enter:duration-300 data-enter:ease-out data-leave:duration-200 data-leave:ease-in sm:my-8 sm:w-full sm:max-w-lg sm:p-6 data-closed:sm:translate-y-0 data-closed:sm:scale-95"
          >
            <div> 
              <div className={clsx(center && "text-center", "  sm:mt-2")}>
                {title && <>
                  <DialogTitle as="h3" className="mb-6  text-base font-semibold text-gray-900">
                    {title}
                  </DialogTitle>
                   <div className={clsx(description&&"mt-2 mb-6")}>
                    <p className="text-sm text-gray-500">
                      {description}
                    </p>
                  </div> 
                  <div className="w-full border-t border-gray-300" />
                </>} 
                {children}
              </div>
            </div>
            <div className="mt-5 sm:mt-4  flex  flex-row-reverse">

              <div>
                <PrimaryButton
                  disabled={isLoading}
                  onClick={() => {
                    confirm();
                  }}
                >{!isLoading && yesButtonText}
                  {isLoading && <Spinner textWhite={true}></Spinner>}
                </PrimaryButton>

              </div>
              <div className='mr-2'>
                <SecondaryButton
                  onClick={() => setIsDialogOpen(false)}
                >{noButtonText}
                </SecondaryButton>
              </div>
            </div>
          </DialogPanel>
        </div>
      </div>
    </Dialog>
  )
});
BaseDialog.displayName = "BaseDialog";
export default BaseDialog;
