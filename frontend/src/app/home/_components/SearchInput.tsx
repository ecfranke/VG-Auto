import { MagnifyingGlassIcon } from "@heroicons/react/24/outline";

export default async function SearchInput({ searchParams,placeholder }: { searchParams: Promise<Record<string, string>>,placeholder:string }) {
    const options = (await searchParams);
    return (
        <div className="  md:p-0 grid   grid-cols-1">
            <input
                name="searchText"
                type="searchText"
                placeholder={placeholder}
                defaultValue={options.searchText}
                aria-label="Search"
                className="col-start-1 row-start-1 block w-full rounded-md bg-white py-1.5 pr-3 pl-10 text-base text-gray-900 outline-1 -outline-offset-1 outline-gray-300 placeholder:text-gray-400 focus:outline-2 focus:-outline-offset-2 focus:outline-indigo-600 pl-9 text-sm/6"
            />
            <MagnifyingGlassIcon
                aria-hidden="true"
                className="pointer-events-none col-start-1 ml-2 row-start-1 size-5 self-center text-gray-400"
            />
        </div>
    )

}