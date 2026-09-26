import GreenBadge from "@/_components/GreenBadge"; 
import { statusNames } from "../../../model";
import YellowBadge from "@/_components/YellowBadge";
import DefaultBadge from "@/_components/DefaultBadge";

export default function WorkStatusBadge({    status }: {   status:string  }){
    return (
        <> 
           {status == 'inprogress'&&<><GreenBadge text= {statusNames[status]} ></GreenBadge></>}
           {status == 'closed'&&<YellowBadge text= {statusNames[status]}></YellowBadge> }
           {status == 'completed'&&<DefaultBadge text= {statusNames[status]}></DefaultBadge> }
        </>
    )
}