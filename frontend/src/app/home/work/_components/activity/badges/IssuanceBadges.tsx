import moment from "moment" 
import GreenBadge from "@/_components/GreenBadge"
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faEnvelopeCircleCheck } from '@fortawesome/free-solid-svg-icons'
 
import RedBadge from "@/_components/RedBadge"
import BlueBadge from "@/_components/BlueBadge"
import { IIssuance, IOfferIssuance, IWorkIssuance } from "../../../model"

const isOverDue = (issuance: IWorkIssuance) => { 
    const dueDate = new Date(issuance.issuedOn);
    dueDate.setDate(dueDate.getDate() + issuance.dueDays);
    return !issuance.isPaid && dueDate <= new Date();
}

export function IssuanceBadges({
    issueance 

}: {
    issueance: IIssuance 
}) {

    const offerIssuance = issueance as IOfferIssuance;
    const workIssuance = issueance as IWorkIssuance;
   
    if(!issueance) throw new Error('issuance null');

    return (
        <> 
           <GreenBadge text='Issued' title={'Issued on ' + moment(issueance.issuedOn, true).locale('en').format('LLL') + ' by ' + issueance.issuedBy} ></GreenBadge>
           {offerIssuance.acceptedOn && <>{' '}<GreenBadge text='Accepted' title={'Accepted on ' + moment(offerIssuance.acceptedOn, true).locale('en').format('LLL') + ' by ' + offerIssuance.acceptedBy} ></GreenBadge></>}
           <EmailSentBadge issueance={issueance}></EmailSentBadge>
           <OverdueBadge issueance={workIssuance}></OverdueBadge>
           {workIssuance.invoiceNumber && workIssuance.isPaid && !isOverDue(workIssuance) && <> <GreenBadge text="Paid"></GreenBadge></>}
           {workIssuance.invoiceNumber && !workIssuance.isPaid && !isOverDue(workIssuance) && <> <BlueBadge text="Unpaid"></BlueBadge></>}
        </>
    )
}


export function OverdueBadge({
    issueance 

}: {
    issueance: IWorkIssuance 
})
{ 
    return (
        <>{issueance?.invoiceNumber && isOverDue(issueance) && <> <RedBadge text="Overdue" ></RedBadge></>}</>
    )
}

export function EmailSentBadge({
    issueance 

}: {
    issueance: IIssuance 
})
{
    return (
        <>{issueance?.sentOn && <span title={'Email sent to ' + issueance.receiverEmail + ' on ' + moment(issueance.sentOn, true).locale('en').format('LLL')}><FontAwesomeIcon icon={faEnvelopeCircleCheck}  size="lg" color='Green' /></span>}</>
    )
}