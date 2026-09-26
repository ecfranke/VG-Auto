import clsx from "clsx";

export function DescriptionItem(
    {
        label,
        value,
        className,
    }: {
        label: string,
        value: string | number | null | undefined,
        className?: string| undefined,
    }
) {
    return (
        <div className="px-1 sm:py-4 py-4 sm:grid sm:grid-cols-3 sm:gap-4 sm:px-0">
            <dt className="text-sm/6 font-medium text-gray-900">{label}</dt>
            {value?<dd className={clsx(className&&className,"mt-1 text-sm/6 text-gray-700 sm:col-span-2 sm:mt-0")}>{value}</dd>:
            <dd className="mt-1  max-w-2xl text-sm/6 text-gray-300 sm:col-span-2 sm:mt-0">(no data)</dd>}
        </div>
    ) 
}