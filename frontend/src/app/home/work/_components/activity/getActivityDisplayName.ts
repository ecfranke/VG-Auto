import { activityNames } from "../../model";


export function getActivityDisplayName(name: string, orderNr: string | number, issuanceNumber?:string|undefined ) {

    if (!name) return '';

    if(issuanceNumber){
        return activityNames[name] + ' nr. ' + issuanceNumber;  //if offer is issued, it gets the number, it does not have it before
    }
     
    if (orderNr === '0') return activityNames[name];
//activity use just local order numbers
    return activityNames[name] + ' (' + orderNr+')';
}
