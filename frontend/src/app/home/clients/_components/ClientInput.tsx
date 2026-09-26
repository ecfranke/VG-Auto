'use client'

import {   useState } from 'react'
import { Field, Label } from '@headlessui/react'
import FormInput from '@/_components/FormInput'; 
import ClientAddress from './ClientAddressInput'; 
import { useRouter } from 'next/navigation';
import ClientEmailsInput from './ClientEmailsInput';
import FormTextArea from '@/_components/FormTextArea';
import PrimaryButton from '@/_components/PrimaryButton';
import SecondaryButton from '@/_components/SecondaryButton';
import FormCheckBox from '@/_components/FormCheckbox';
import FormSwitch from '@/_components/FormSwitch';  
import { IClientData } from '../model';
import BlueBadge from '@/_components/BlueBadge';


export default function ClientInput({
    client
}: {
    client?: IClientData | undefined
}) {

    const router = useRouter()
     
    const [isCompany, setIsCompany] = useState(!client || client?.isPrivate ? false : true);

    //TODO, makes sense to make validation it more generic?
    const [companyNameRequired, setCompanyNameError] = useState("");
    const [firstNameRequired, setFirstNameError] = useState("");

    const [companyName, setCompanyName] = useState(client?.name);
    const [firstName, setFirstName] = useState(client?.firstName);
    
    function validate(event: React.MouseEvent) {

        if (isCompany && !companyName) {
            setCompanyNameError("Company name required");
            event.preventDefault();
        }
        if (!isCompany && !firstName) {
            setFirstNameError("Firstname is required");
            event.preventDefault();
        } 
    }

    

    return (
        <>
            <div className="space-y-12">
                <div className="border-b border-gray-900/10 pb-12">
                   <Field className="flex items-center mb-4 ">
                            <FormSwitch 
                            name='isCompany' 
                            checked={isCompany}
                            onChange={(value) => {
                                setIsCompany(value);
                            }}></FormSwitch> 

                            <Label as="span" className="ml-3 text-sm">
                                <BlueBadge  text={isCompany ? 'Company' : 'Private person'}></BlueBadge>{' '}
                            </Label>
                        </Field>
                    <div className=" grid grid-cols-1 gap-x-6 gap-y-8 sm:grid-cols-6">

                        {isCompany ?
                            <div className="sm:col-span-3">
                                <FormInput name='name'
                                    inputError={companyNameRequired}
                                    onInputChange={(e) => setCompanyName(e.currentTarget.value)}
                                    defaultValue={companyName}
                                    label='Company name'></FormInput>
                            </div> : <>
                                <div className="sm:col-span-3">
                                    <FormInput name='first-name'
                                      inputError={firstNameRequired}
                                      onInputChange={(e) => setFirstName(e.currentTarget.value)}
                                     defaultValue={firstName} label='First name'></FormInput>
                                </div>
                                <div className="sm:col-span-3">
                                    <FormInput name='last-name' defaultValue={client?.lastName} label='Last name'></FormInput>
                                </div>
                            </>
                        }

                        <div className="sm:col-span-3">
                            {isCompany ? <FormInput name='regNr' defaultValue={client?.regNr} label='Registry code'></FormInput>
                                : <FormInput name='personal-code' defaultValue={client?.personalCode} label='Personal code'></FormInput>}

                        </div>
                        <div className="sm:col-span-3">
                            <FormInput name='phone' defaultValue={client?.phone} label='Phone'></FormInput>
                        </div> 
                        <ClientEmailsInput client={client}></ClientEmailsInput>
                        <ClientAddress address={client?.address} name='street-address'></ClientAddress>
                    </div>
                </div>
            </div>
            <div className="border-b border-gray-900/10 pb-12">
                <div className="mt-4 grid grid-cols-1 gap-x-6 gap-y-8 sm:grid-cols-6">
                    <div className="col-span-full">
                        <FormCheckBox name='complicated' label='Complicated client' defaultChecked={client?.isAsshole}></FormCheckBox>
                    </div>
                    <div className="col-span-full">
                        <FormTextArea name='about' label='About'  defaultValue={client?.description}>
                        </FormTextArea> 
                    </div>
                </div>
            </div>
            <div className="mt-6 flex items-center justify-end gap-x-6"> 
                <SecondaryButton  onClick={() => router.back()}>Cancel</SecondaryButton>
                <PrimaryButton  onClick={validate}>Save</PrimaryButton> 
            </div> 
        </>
    )
}