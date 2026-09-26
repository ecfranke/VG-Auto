export interface IClientData {
    name: string;
    firstName: string;
    lastName: string;
    regNr: string;
    personalCode: string;
    isAsshole: boolean;
    id: string;
    address: IAddressData;
    phone: string;
    emailAddresses: string[];
    currentEmail: string;
    ssAsshole: boolean;
    description: string;
    introducedAt: string;
    isPrivate: boolean;
}

export interface IAddressData {
    street: string,
    city: string,
    region: string,
    postalCode: string,
    country: string
  }