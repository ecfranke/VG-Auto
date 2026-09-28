import Search from "../_components/Search";
import moment from "moment";
import { IOfferIssuance, IWorkIssuance } from "./model";
import PricingDownloadLink from "./_components/activity/PricingDownloadLink";
import { ArrowDownTrayIcon } from "@heroicons/react/20/solid";
import Spinner from "@/_components/Spinner";
import BlueBadge from "@/_components/BlueBadge";
import WorkStatusBadge from "./_components/activity/badges/WorkStatusBadge";
import { EmailSentBadge, OverdueBadge } from "./_components/activity/badges/IssuanceBadges";
import { SearchCardHeader } from "../_components/SearchCardHeader";
import { Card } from "@/_components/Card";
import SearchStatusFilter from "./_components/SearchStatusFilter";
import SearchParams from "./_components/SearchParams"; 
import PrimaryButton from "@/_components/PrimaryButton";
import SearchInput from "../_components/SearchInput";
import FormInput from "@/_components/FormInput";
import { vehicleTitle } from "@/_lib/shared/vehicle";

export default async function Page(
  { searchParams }: { searchParams: Promise<Record<string, string>> }) {

  const options = (await searchParams);

  const isInvoiceView = options.issued == 'on';

  // type of the work: repair job and/or offer (the invoice in the invoice view)
  const typeColumn = isInvoiceView ? {
    dataField: 'issuance',
    headerText: 'Type',
    dataFormatter: ({ issuance, id }: { issuance: IWorkIssuance, id: string }) => {
      return (
        issuance ?
          <div className="flex gap-x-2 ">
            <div>  <PricingDownloadLink
              name='Invoice'
              id={id}
              number={issuance.invoiceNumber}
              downloadingElement={<Spinner></Spinner>}
              hidePaperClip={true}
              clickableElement={<ArrowDownTrayIcon aria-hidden="true" className="h-6 w-5 text-gray-400" ></ArrowDownTrayIcon>} >
            </PricingDownloadLink> </div>
          </div> :
          <></>
      );
    }
  } : {
    dataField: 'offerissuance',
    headerText: 'Type',
    dataFormatter: ({ offerIssuance, hasRepairs, numberOfOffers }: { hasRepairs: boolean, offerIssuance: IOfferIssuance, numberOfOffers: number }) => {
      return (
        <div className="flex gap-x-2">
          {hasRepairs && <BlueBadge text="Repair job"></BlueBadge>}
          {numberOfOffers > 1 ?
            <BlueBadge text="Many offers"></BlueBadge> :
            <>
              {offerIssuance &&
                <>
                  <div>  <PricingDownloadLink
                    name='Offer'
                    id={offerIssuance.id}
                    number={offerIssuance.number}
                    downloadingElement={<Spinner></Spinner>}
                    hidePaperClip={true}
                    hideLabel={false}
                    clickableElement={<ArrowDownTrayIcon aria-hidden="true" className="h-6 w-5 text-gray-400" ></ArrowDownTrayIcon>} >
                  </PricingDownloadLink> </div>
                  <div> <h5><EmailSentBadge issueance={offerIssuance}></EmailSentBadge></h5></div>
                </>
              }
              {!offerIssuance && numberOfOffers === 1 && <BlueBadge text="Offer"></BlueBadge>}
            </>
          }</div>
      );
    }
  };

  const columns = [
    {
      dataField: 'workNr',
      headerText: 'Work',
      dataFormatter: ({ id, workNr }: { id: string, workNr: string }) => {
        return (
          <a href={'/home/work/' + id}>
            <h5 className="whitespace-nowrap">Work nr. {workNr}</h5>
          </a>
        );
      }
    },
    typeColumn,
    {
      dataField: 'status',
      headerText: 'Status',
      dataFormatter: ({ status, issuance }: { status: string, issuance: IWorkIssuance }) => {
        return (
          <div className="flex gap-x-1">
            <WorkStatusBadge status={status}></WorkStatusBadge>
            {isInvoiceView && issuance && <><EmailSentBadge issueance={issuance}></EmailSentBadge><OverdueBadge issueance={issuance}></OverdueBadge></>}
          </div>
        );
      }
    },
    {
      dataField: 'clientId',
      headerText: 'Client',
      dataFormatter: ({ clientName, clientId }: { clientName: string, clientId: string }) => {
        return (
          <a href={'/home/clients/' + clientId} >
            <h5 >{clientName}</h5>
          </a>
        );
      }
    },
    {
      dataField: 'vehicleId',
      headerText: 'Vehicle',
      dataFormatter: ({ licensePlate, vehicleId, vehicleManufacturer, vehicleModel, vehicleYear }: { licensePlate: string, vehicleId: string, vehicleManufacturer?: string, vehicleModel?: string, vehicleYear?: number }) => {
        if (!vehicleId) return <></>;
        const title = vehicleTitle({ year: vehicleYear, manufacturer: vehicleManufacturer, model: vehicleModel });
        return (
          <a href={'/home/vehicles/' + vehicleId} >
            <h5 className="mb-0 fs--1">{licensePlate || title}</h5>
            {licensePlate && title && <p className="text-xs text-gray-500">{title}</p>}
          </a>
        );
      }
    },
    {
      dataField: 'mechanicNames',
      headerText: 'Mechanics',
    },
    {
      dataField: 'startedOn',
      headerText: 'Start date',
      dataFormatter: ({ startedOn }: { startedOn: Date }) => {
        return (
          <span className="whitespace-nowrap">{moment(startedOn, true).format('LL')}</span>
        );
      }
    },
    {
      dataField: 'notes',
      headerText: 'Note',
      dataFormatter: ({ notes }: { notes: string }) => {
        return (
          <p title={notes} className="truncate" style={{ maxWidth: '300px', marginBottom: "-5px" }} >
            {notes}
          </p>
        );
      }
    }
  ]


  return <main className=" lg:pl-62   ">
    <form method="GET" >
      <div className=" sm:py-6 px-4 sm:px-8   sm:gap-4">


        <div className="">

          <Card header={
            
            <SearchCardHeader title="Find Work" pageName="work">
            </SearchCardHeader>}  >

            <Search
              searchParams={searchParams}
              resourceName="work"
              idField="id"
              rowClass={(item) => {
                return (item['status'] === 'closed' ? 'line-through' : '')
              }}
              columns={columns}> 
              <div className=" 3xl:flex">
                 <div className="  grid grid-cols-1  md:grid-cols-12 md:grid-flow-row md:gap-x-2 3xl:grid-flow-col  3xl:grid-cols-24   p-0 3xl:gap-x-2  gap-y-2  "> 
                      <div className="3xl:col-span-6 md:col-span-7 "   >
                        <SearchStatusFilter issued={options.issued === 'on'} status={options.status}></SearchStatusFilter>
                        <SearchInput searchParams={searchParams} placeholder="number, client, VIN or license plate" ></SearchInput> 
                      </div> 
                      <div className="3xl:col-span-4  md:col-span-5 ">
                         <FormInput name="saleable" label="Product or service" placeholder="code or name ..." defaultValue={options.saleable}  ></FormInput>
                      </div>
                      <div  className="3xl:col-span-14  md:col-span-12  " >
                      <SearchParams options={options}></SearchParams>
                      </div>
                      
                  </div> 
                  <div className="mx-2 text-right mt-8">
                        <PrimaryButton   id="btnSubmit">Search</PrimaryButton>
                   </div>
              </div>
                
            </Search>
          </Card>

        </div>
      </div>
    </form>
  </main>
}