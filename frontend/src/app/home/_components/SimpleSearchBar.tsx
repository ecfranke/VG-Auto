import PrimaryButton from "@/_components/PrimaryButton";
import SearchInput from "./SearchInput";

export default async function SimpleSearchBar({ searchParams,placeholder }: { searchParams: Promise<Record<string, string>>,placeholder:string }) {

  return (
    <div className=" sm:px-0 space-y-12 gap-x-2">
      <div className="  grid grid-cols-1 sm:grid-flow-col gap-x-6 gap-y-2 sm:gap-y-8  ">
        <div>
          <SearchInput searchParams={searchParams} placeholder={placeholder} ></SearchInput>
        </div>
        <div >
          <PrimaryButton className="float-right" id="btnSubmit">Search</PrimaryButton>
        </div>
      </div>
    </div>
  )

}