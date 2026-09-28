import type { ITaxOptions } from '@/_lib/shared/taxes';

export interface IUserOptions {
    requisites: Requisites;
    pricing:    Pricing;
}

export interface Pricing {
    invoice:  Invoice;
    estimate: Estimate;
    currency: string;
    taxes:    ITaxOptions | null;
}

export interface ICurrency {
    code:     string;
    name:     string;
    decimals: number;
    culture:  string;
}

export interface Estimate {
    emailContent: string;
}

export interface Invoice {
    vatRate:       number;
    surCharge:     string;
    disclaimer:    string;
    signatureLine: boolean;
    emailContent:  string;
}

export interface Requisites {
    name:        string;
    phone:       string;
    address:     string;
    email:       string;
    bankAccount: string;
    regNr:       string;
    kmkr:        string;
}
