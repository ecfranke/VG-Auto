export default async function Narrow({
    children
}:{
    children: React.ReactNode
}) {
    return (
      <div className="mx-auto max-w-7xl xl:px-4  ">
        {/* We've used 3xl here, but feel free to try other max-widths based on your needs */}
        <div className="mx-auto max-w-2xl">{children}</div>
      </div>
    )
  }

  