import { activityNames } from "../../model";


export function getActivityDisplayName(name: string, orderNr: string | number, issuanceCode?:string|undefined ) {

    if (!name) return '';

    if(issuanceCode){
        return activityNames[name] + ' ' + issuanceCode;  //an issued offer is named by its estimate, like the work: Offer OF_TF_2019_HC_2026_09_28_15
    }
     
    if (orderNr === '0') return activityNames[name];
//activity use just local order numbers
    return activityNames[name] + ' (' + orderNr+')';
}
