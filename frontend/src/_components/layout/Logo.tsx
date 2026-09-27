import Image from "next/image" 

export function Logo(props: { width: number; height: number; className: string; }) {
    
  return (
    <Image alt="VG Auto" {...props}  src="/logo.png"   ></Image>
  )
}