import clsx from "clsx";
import Search from "../_components/Search";
import 'car-makes-icons/dist/style.css';
import { SearchCardHeader } from "../_components/SearchCardHeader";
import Main from "../_components/Main";
import SimpleSearchBar from "../_components/SimpleSearchBar";


export default async function Page(
  { searchParams }: { searchParams: Promise<Record<string, string>> }) {

  return <Main header={
    <SearchCardHeader title="Find Vehicles" pageName="vehicles">
    </SearchCardHeader>
  } narrow={false}>
     <form method="GET" > <Search
      searchParams={searchParams}
      resourceName="vehicles"
      columns={[

        {
          dataField: 'manufacturer',
          headerText: 'Manufacturer',
          dataClasses: () => {
            return "pl-4 font-medium gray-900 whitespace-nowrap";
          },
          dataFormatter: ({ manufacturer }) => {
            const producerName = (manufacturer ?? '').trim().replace(" ", "-").toLowerCase();
            return (
              <div className="flex items-center " >
                <i className={clsx("pr-2 text-2xl", "car-" + producerName)}>  </i>
                <span className="text-sm">{manufacturer}</span>
              </div>
            );
          }
        },
        {
          dataField: 'model',
          headerText: 'Model',
        },
        {
          dataField: 'year',
          headerText: 'Year',
        },
        {
          dataField: 'licensePlate',
          headerText: 'License plate',
          dataFormatter: ({ licensePlate, id }) => {
            return (
              <a href={'/home/vehicles/' + id} >
                <h5 className="font-semibold"> {licensePlate}</h5>
              </a>
            );
          }
        },
        {
          dataField: 'ownerName',
          headerText: 'Owner',
          dataFormatter: ({ ownerName, ownerId }) => {
            if (!ownerName) return <p className="font-italic text-gray-400">No owner</p>;
            return (
              <a href={'/home/clients/' + ownerId} >
                <h5 >{ownerName}</h5>
              </a>
            );
          }
        },
        {
          dataField: 'vin',
          headerText: 'VIN',
          dataFormatter: ({ vin, id }) => {
            return (
              <a href={'/home/vehicles/' + id} >
                <h5  >{vin}</h5>
              </a>
            );
          }
        }
      ]}>
        <SimpleSearchBar searchParams={searchParams} placeholder="VIN, license plate, owner or manufacturer ..."></SimpleSearchBar> 
        </Search></form>
   
  </Main>



}