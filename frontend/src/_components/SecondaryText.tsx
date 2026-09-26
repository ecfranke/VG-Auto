
export function SecondaryText({
    children
}: {
    children: React.ReactNode;
}) {
    return (
        <span className="mt-1 max-w-2xl text-sm/6 text-gray-500 whitespace-pre-line">{children}</span>
    );
}
