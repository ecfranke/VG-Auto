import { Card } from "@/_components/Card" 
import Narrow from "./Narrow"

export default async function Main({
    header,
    children,
    narrow=true
}:{
    narrow?:boolean | undefined
    children: React.ReactNode,
    header: React.ReactNode
}) {
    return (
       <> 
                <main className=" lg:pl-62 pb-8">
                    <div className="  px-4 sm:py-10 sm:px-6 lg:px-8 lg:py-6 ">
                        {narrow?<Narrow>
                            <Card header={header}  >  {children}</Card> 
                        </Narrow>:
                         <Card header={header}  >  {children}</Card> 
                        }
                        
                    </div>
                </main> 
                </>
    )
}