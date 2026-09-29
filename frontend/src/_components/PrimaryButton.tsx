import clsx from "clsx"

export interface IButtonClick {
    (event: React.MouseEvent): void
}


export default function PrimaryButton({
    id,
    children,
    onClick,
    className,
    disabled,
}: {
    id?:string|undefined,
    children: React.ReactNode,
    onClick?: IButtonClick,
    className?: string | undefined,
    disabled?: boolean | undefined
}) {
    return (
        <button
            id={id}
            disabled={disabled}
            type="submit"
            onClick={onClick}
            className={clsx(className, "rounded-md bg-primary px-3 py-2 text-sm font-semibold text-white shadow-xs hover:bg-primary-hover focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary")}
        >
            {children}
        </button>
    )
}