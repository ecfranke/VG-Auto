export default function GreenBadge({
    
    text, 
    title, 
}: {  
    text: string
    title?: string, 
}) { 
    return (
        <span title={title} className="inline-flex items-center rounded-md bg-green-50 px-2 py-1 text-xs font-medium text-green-700 ring-1 ring-green-600/20 ring-inset"> 
                {text}</span>
    )
}