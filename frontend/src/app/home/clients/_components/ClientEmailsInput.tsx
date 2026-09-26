import {
    EnvelopeIcon
} from '@heroicons/react/20/solid' 
import { useState } from 'react'; 
import FormInput from '@/_components/FormInput';
import { Field } from '@headlessui/react'
import FormSwitch from '@/_components/FormSwitch';
import FormLabel from '@/_components/FormLabel';
import FormInputWithButton from '@/_components/FormInputWithButton';
import FormList from '@/_components/FormList';
import React from 'react'; 
import { IClientData } from '../model';
import BlueBadge from '@/_components/BlueBadge';

export default function ClientEmailsInput({
    client
}: {
    client?: IClientData | undefined
}) {

    const [emails, setEmails] = useState(client?.emailAddresses ?? [])
    const [selectedEmail, setSelectedEmail] = useState("");
    const [currentEmail, setCurrentEmail] = useState(client?.currentEmail);
    const [usesMultipleEmails, setUsesMultipleEmails] = useState(emails.length > 1) 
    return (
        <>
         <input type="hidden" name='currentEmail' defaultValue={currentEmail} ></input>
        {!usesMultipleEmails?
           <> <div className='sm:col-span-4'>
                <FormInput name='emails' defaultValue={currentEmail} onInputChange={
                    (e)=>{
                        setCurrentEmail(e.currentTarget.value);
                    }
                }  label='Email address'></FormInput> 
            </div> 
            <div className='sm:col-span-2'> 
                <FormLabel name='emails' label='Use multiple emails'></FormLabel>
                <div className="mt-3  items-center flex">
                    <Field>
                        <FormSwitch 
                          checked={usesMultipleEmails}
                          onChange={(checked)=>{
                              setUsesMultipleEmails(checked);
                          }}
                        ></FormSwitch> 
                    </Field>
                </div>
            </div></>: 
            <><div className='sm:col-span-4'> 
               <FormInputWithButton 
                      name='selectEmail' 
                      label='Email address(es)'
                      defaultValue={selectedEmail}
                      onInputChange={(e) => {
                        setSelectedEmail(e.currentTarget.value);
                      }}
                      onButtonClick={() => {
                        const newEmails = [...emails, selectedEmail];
                        setEmails(newEmails); 
                        if (newEmails.length == 1) {
                            setCurrentEmail(newEmails[0])
                        }
                     }}>
                    
               </FormInputWithButton> 
            </div>
                <div className='sm:col-span-2'> 
                    <FormLabel name='emails' label='Turn off multiple emails'></FormLabel>
                    <div className="mt-3  items-center flex">
                        <Field>
                            <FormSwitch
                             checked={usesMultipleEmails}
                             onChange={(e) => {
                                 setUsesMultipleEmails(e);
                                 const newEmails =currentEmail?[currentEmail as string]:[] ;
                                 setEmails(newEmails);
                             }}> 
                            </FormSwitch> 
                        </Field>
                    </div>
                </div>
                {emails.length>0&& <div className='col-span-full'>
                    <FormList items={emails}  renderItem={ (item)=>{
                        const mail = item;
                        const isPrimary = mail == currentEmail
                        return (
                          <>
                            <div className="flex w-0 flex-1 items-center">
                                <EnvelopeIcon aria-hidden="true" className="size-5 shrink-0 text-gray-400" />
                                <div className="ml-4 flex min-w-0 flex-1 gap-2">
                                    <span className="truncate font-medium">{mail}</span>
                                    {isPrimary && <BlueBadge text='Primary' ></BlueBadge>}  
                                </div>
                            </div>
                            <input type="hidden" name='emails' defaultValue={mail} ></input>
                            <div className="ml-4 flex shrink-0 space-x-4">
                                {!isPrimary &&
                                    <button
                                        onClick={() => {
                                            setCurrentEmail(mail);
                                        }}
                                        type="button"
                                        className="rounded-md bg-white font-medium text-indigo-600 hover:text-indigo-500">
                                        Set as primary
                                    </button>}
                                <span aria-hidden="true" className="text-gray-200">
                                    |
                                </span>
                                <button type="button"
                                    onClick={() => {
                                        emails.splice(emails.indexOf(mail), 1);
                                        setEmails([...emails]);
                                        if (isPrimary && emails.length > 0) {
                                            setCurrentEmail(emails[0])
                                        } 
                                    }}
                                    className="rounded-md bg-white font-medium text-gray-900 hover:text-gray-800">
                                    Remove
                                </button>
                            </div>
                          </>
                        )
                    }} > 
                    </FormList> 
                </div>} 
                </>
            }
        </>
    )
}