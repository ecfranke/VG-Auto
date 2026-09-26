import { SecondaryText } from "./SecondaryText"

export default function FormTitle({
    title,
    description,
    children
}: {
    children?: React.ReactNode,
    title:string,
    description?:string | undefined
}) {
    return (
        <>
            <h3 className="text-base/7 font-semibold text-gray-900">{title} 
            </h3>
            {description&&<SecondaryText>{description}</SecondaryText>}
            {children} 
        </>
    )
}

