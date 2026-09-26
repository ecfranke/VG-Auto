export default function FormLabel({
    name,
    label,
    children,
}:{
    name:string,
    label:string,
    children?:React.ReactNode
}){
    return (
        <label htmlFor={name} className="block text-sm/6 font-medium text-gray-900">
        {label}{children}
    </label>
    )
}