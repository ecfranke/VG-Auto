 
export default function RedBadge({
    
    text, 
    title, 
}: {  
    text: string
    title?: string, 
}) { 
    return (
        <span title={title} className="inline-flex items-center rounded-md bg-red-50 px-2 py-1 text-xs font-medium text-red-700 ring-1 ring-red-600/10 ring-inset"> 
                {text}</span>
    )
}
