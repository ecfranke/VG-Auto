import clsx from "clsx"
import { IButtonClick } from "./PrimaryButton"

export default function SecondaryButton({
    children,
    onClick,
    className,
}: {
    children: React.ReactNode,
    onClick: IButtonClick,
    className?: string | undefined
}) {
    return (
        <button type="button" onClick={(e) => onClick(e)} className={clsx(className, "rounded-md bg-white px-3 py-2 text-sm font-semibold text-gray-900 ring-1 shadow-xs ring-gray-300 ring-inset hover:bg-gray-50")}>
            {children}
        </button>
    )
}