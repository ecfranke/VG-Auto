

'use client'
import {  IWorkData } from '../model';
import { DocumentTextIcon,   TruckIcon, UserCircleIcon, WrenchScrewdriverIcon } from '@heroicons/react/20/solid';
import moment from 'moment';
import React from 'react';
import { startAnActivity } from '../actions/startAnActivity';
import ButtonGroup, { IButtonOption } from '@/_components/ButtonGroup';
import HamburgerMenu from '@/_components/HamburgerMenu'; 
import { BaseDialogHandle } from '@/_components/BaseDialog';
import IssueInvoiceDialog from './activity/IssueInvoiceDialog';
import DeleteInvoiceDialog from './activity/DeleteInvoiceDialog';
import PricingDownloadLink from './activity/PricingDownloadLink';
import WorkStatusBadge from './activity/badges/WorkStatusBadge';
import { IssuanceBadges } from './activity/badges/IssuanceBadges';
import { togglePaid } from '../actions/togglePaid';
import SendPricingDialog from './activity/SendPricingDialog';
import { deleteWork } from '../actions/deleteAnActivity';
import ConfirmDialog, { ConfirmDialogHandle } from '@/_components/ConfirmDialog';
import { createACopy } from '../actions/createACopy';
import FormSwitch from '@/_components/FormSwitch'; 
import { Field, Label } from '@headlessui/react';
import { changeWorkStatus } from '../actions/changeWorkStatus';


export function WorkInformation({
    work,
    hasRepairJobWithProductsOrServices,
}: {
    work: IWorkData,
    hasRepairJobWithProductsOrServices: boolean
}) {

    const editPath = '/home/work/edit/' + work.id;
 
    const vehicleSummary = [work.vehicleProducer, work.vehicleModel, work.vehicleVin, work.vehicleRegNr].filter(x => x).join(', ');
    const clientSummary = [work.clientName, work.clientPhone, work.clientEmail].filter(x => x).join(', ');

    const deleteInvoiceRef = React.useRef<BaseDialogHandle>(null);
    const createInvoiceRef = React.useRef<BaseDialogHandle>(null);
    const sendInvoiceRef = React.useRef<BaseDialogHandle>(null);
    const deleteWorkRef = React.useRef<ConfirmDialogHandle>(null);
    
    const workMenuOptions = work.issuance ? [] : [
        { name: 'Make an offer', onClick: async () => { await startAnActivity(work.id, "offer") } },
        { name: 'Start repair job', onClick: async () => { await startAnActivity(work.id, "repairjob") } },
        { name: 'Edit', href: editPath },
        { name: 'Delete', redText:true, onClick:   () => { deleteWorkRef.current?.open({
            title:'Delete work',description:'Are you sure you want to delete it?',confirmObj:work.id
        })  } },
    ] as IButtonOption[];
   
    workMenuOptions.push({ name: 'Create a copy', onClick: async () => { await createACopy(work.id) } })
        ;

    const issuedButtonOptions = !work.issuance? []: [
        {
            name: 'Delete invoice',
            isPrimary:false,
            redText:true,
            inMenu:true,
            onClick:() => { deleteInvoiceRef.current?.open() }
        },
        {
            name: (work.issuance.isPaid?'Unpaid':'Payment received'),
            isPrimary:!work.issuance.isPaid,
            onClick:async () => { await togglePaid(work.id,!work.issuance.isPaid) }
        } , {
            name: 'Send invoice',
            isPrimary:false,
            onClick:() => { sendInvoiceRef.current?.open() }
        },
    ] as IButtonOption[];

    const editButtonOptions = []  as IButtonOption[];

    if(!work.issuance){

      
        if(work.status!=='closed'){
            editButtonOptions.push({ 
                name: 'Close',
                onClick: async() => { 
                    await changeWorkStatus(work.id,'Closed');
                }, 
             });
        }
        else if(work.status==='closed'){
            editButtonOptions.push({ 
                name: 'Open',
                isPrimary: true,
                onClick: async() => { 
                    await changeWorkStatus(work.id,'Default');
                }, 
             });
        }

        if(hasRepairJobWithProductsOrServices && work.status!=='closed' ){
            editButtonOptions.push({
                name: 'Issue invoice',
                onClick:() => { createInvoiceRef.current?.open() },
                isPrimary:true 
            });
        }
       
    }
    
    return (
        <>
            <IssueInvoiceDialog work={work} dialogRef={createInvoiceRef}></IssueInvoiceDialog>
            <DeleteInvoiceDialog work={work} dialogRef={deleteInvoiceRef}></DeleteInvoiceDialog>
            <SendPricingDialog work={work} dialogRef={sendInvoiceRef}></SendPricingDialog>
            <ConfirmDialog ref={deleteWorkRef} onConfirm={async ()=>{
                await deleteWork(work.id) ;
            }} ></ConfirmDialog>
            <div className="lg:col-start-3 lg:row-end-1">
                <h2 className="sr-only">Summary</h2>
                <dl className="flex flex-wrap">
                    <div className="flex-auto xl:pt-6 xl:pl-6">
                        <dt className="text-base font-semibold text-gray-900 mr-2">Work nr {work.number}{' '}
                            <WorkStatusBadge   status={work.status}></WorkStatusBadge> 
                        </dt>
                        <dd className="text-sm/6 text-gray-500">
                            <time dateTime="2023-01-31">{moment(work.startedOn).format('LLL')}</time>
                        </dd>
                    </div>
                     
                    <div className="flex-none  col-span-2 self-end px-2 xl:px-6 pt-4">
                        <dt className="sr-only"></dt>
                        <dd className="inline-flex  ">
                            <HamburgerMenu options={workMenuOptions}></HamburgerMenu>
                        </dd>
                    </div>
                    <div className="mt-6 flex w-full flex-none gap-x-4 border-t border-gray-900/5 px-6 pt-6"></div>
                    {clientSummary &&
                        <div className="mt-4 flex w-full flex-none gap-x-4 xl:px-6">
                            <dt className="flex-none">
                                <span className="sr-only">Client</span>
                                <UserCircleIcon aria-hidden="true" className="h-6  w-5 text-gray-400" />
                            </dt>
                            <dd className="text-sm/6 font-medium text-gray-900">
                                <span>{clientSummary}</span>
                            </dd> </div>
                    }

                    {vehicleSummary && <div className="mt-4 flex w-full flex-none gap-x-4 xl:px-6">
                        <dt className="flex-none">
                            <TruckIcon aria-hidden="true" className="h-6 w-5 text-gray-400" />
                        </dt>
                        <dd className="text-sm/6 text-gray-500">
                            <time dateTime="2023-01-31">{vehicleSummary}</time>
                        </dd>
                    </div>}
                    {work.mechanics?.length > 0 &&
                        <div className="mt-4 flex w-full flex-none gap-x-4 xl:px-6">
                            <dt className="flex-none">
                                <span className="sr-only">Status</span>
                                <WrenchScrewdriverIcon aria-hidden="true" className="h-6 w-5 text-gray-400" />
                            </dt>
                            <dd className="text-sm/6 text-gray-500">{work.mechanics.map((item) => item.name).join(', ')}</dd>
                        </div>
                    }
                    {work.notes && <div className="mt-4 flex w-full flex-none gap-x-4 xl:px-6">
                        <dt className="flex-none">
                            <span className="sr-only">Notes</span>
                            <DocumentTextIcon aria-hidden="true" className="h-6 w-5 text-gray-400" />
                        </dt>
                        <dd className="text-sm/6 text-gray-500 whitespace-pre-line">{work.notes}</dd>
                    </div>}
                    <div className="mt-6 flex gap-x-2 xl:px-6   ">  
                        {work.issuance && <>
                            
                        <PricingDownloadLink name='Invoice' id={work.id} hidePaperClip={false} number={work.issuance.invoiceNumber}></PricingDownloadLink>
                        <IssuanceBadges issueance={work.issuance}   ></IssuanceBadges></> } 
                    </div>

                    <div className="mt-6 flex w-full xl:px-6 ">
                        <dt className=" flex-auto">
                       {!work.issuance && work.status!=='closed'&&<Field className="flex mt-1 items-center"> 
                            <FormSwitch 
                               name='inprogress' 
                               defaultChecked={work.status === 'inprogress'} 
                               onChange={async (val)=>{
                                   const status = val? 'InProgress':'Default';
                                    await changeWorkStatus(work.id,status);
                               }}
                               >
                               </FormSwitch>
                            <Label as="span" className="ml-3 text-sm"> 
                                <span className="text-gray-500">Is in progress</span>
                            </Label>
                            </Field> } 
                        </dt>
                        <dd>
                            
                            {work.issuance ?
                                <ButtonGroup options={issuedButtonOptions}></ButtonGroup>:
                                <ButtonGroup options={editButtonOptions}></ButtonGroup>
                                }
                        </dd>
                    </div>

                </dl>

            </div>
        </>

    )
}