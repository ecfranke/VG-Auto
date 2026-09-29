export interface IWorkData extends IActivity{ 
    id:              string;
   // number:          string;
   // starterName:     string;
    clientId:        string;
    clientName:      string;
    clientAddress:   string;
    clientEmail:     string;
    clientPhone:     string;
    vehicleId:       string;
    /** readable work number, e.g. RP_TF_2019_HC_2026_09_28_15 */
    code: string;
    vehicleManufacturer: string;
    vehicleModel:    string;
    vehicleYear:     number | null;
    vehicleTrim?:    string | null;
    vehicleVin:      string;
    vehicleLicensePlate: string;
    notes:           string;
    odo:             number;
    mechanics:       IMechanic[];
    status:          string;
    issuance:       IWorkIssuance;  
    
}

export interface ICurrentActivity{
    id:        string;
    notes:     string;
    isVehicleLinesOnPricing: boolean;
    products: IProduct[];
    priceSummary: IPriceSummary
    /** currency of the issued document, otherwise the company currency */
    currency: string
}

export interface IActivity{
    id:        string;
    number:    string;
    startedOn: Date;
    startedBy: string;
    name:      string;
    isEmpty: boolean;
}

export interface IWorkIssuance extends IIssuance{
    invoiceNumber: number;
    /** the invoice, named like the work: RP_TF_2019_HC_2026_09_28_15 */
    code: string;
     dueDays: number;
     isPaid:boolean;
}

export interface IOfferIssuance extends IIssuance{
    id: string,
    number: string,
    /** the estimate, named like the work: OF_TF_2019_HC_2026_09_28_15 (a later offer …_15-1) */
    code: string,
    acceptedOn?: Date;
    acceptedBy?: string;
    /** the client signed the estimate online (link in the estimate email) */
    signedOn?: Date;
    signedBy?: string;
}

export interface IIssuance{
  
    sentOn?: Date;
    issuedOn: Date;
    issuedBy: string; 
    receiverEmail?: string 

}
export interface IActivities
{ 
    items: IActivity[]
    current: ICurrentActivity
}

export interface IPriceSummary{
    totalWithVat: number,
    totalWithoutVat:number,
    /** each tax on the subtotal (GST, PST ...) */
    taxes?: { name: string, rate: number, amount: number }[]
}

export interface IInvoice {
    isIssued: boolean;
}

export interface IStatus {
    startedOn:     Date;
    invoiceIssued: boolean;
    offers:        IOffer[];
}

export interface IOffer {
    number:   string;
    isIssued: boolean;
}

export interface IProduct
{
    id:       string;
    name:     string;
    quantity: number | null;
    unit:     string;
    price:    number| null;
    discount: number| null;
    code:     string;
    
}
export interface IMechanic{
    id: string,
    name: string,
}

export interface IActivityNames{
    [key: string]: string
    offer: string,
    repairjob: string,
  }
  export interface IStatusNames{
    [key: string]: string
    closed: string,
    inprogress: string,
    completed: string,
  }
 export const activityNames = {
    offer: 'Offer',
    repairjob:'Repair job' 
  } as IActivityNames

  export const statusNames = {
    default: 'Open',
    closed: 'Closed',
    inprogress:'In Progress',
    completed:'Completed'
  } as IStatusNames
    
  export interface IPaymentNames{
    [key: string]: string
  }

  export const paymentTypes ={ 
    cash:'Cash',
    banktransfer:'Bank transfer',
    cardpayment:'Card payment'
  } as IPaymentNames
