 
CREATE OR REPLACE FUNCTION f_concat_ws(text, VARIADIC text[])
  RETURNS text LANGUAGE sql IMMUTABLE AS 'SELECT array_to_string($2, $1)';
  
CREATE SCHEMA domain;

CREATE TABLE domain.Employee (
	Id uuid primary key,
    FirstName varchar NOT NULL,
    LastName varchar NOT NULL,
	Email varchar,
	Phone varchar,
	Address varchar,
	Proffession varchar,
	Description varchar,
	IntroducedAt timestamp without time zone not null
);
 
CREATE TABLE domain.Vehicle (
	Id uuid primary key,
    Producer VARCHAR,
	Model VARCHAR,
    RegNr VARCHAR not null,
    Vin VARCHAR,
    Odo INT,
    Body varchar,
	DrivingSide varchar,
	Engine varchar,
	ProductionDate date,
	Region varchar,
	Series varchar,
	Transmission varchar,
	Description varchar,
	IntroducedAt timestamp with time zone not null
);


CREATE TABLE domain.Client (
	Id uuid primary key,
	Address varchar,
	Country varchar,
	Region varchar,
	City varchar,
	PostalCode varchar,
	Phone varchar , 
	Description varchar,
	IsAsshole boolean default false NOT NULL,
	IntroducedAt timestamp with time zone not null
);
 

CREATE table domain.VehicleRegistration (
	OwnerId uuid NOT NULL references domain.Client, 
	VehicleId uuid not null references domain.Vehicle ,
	DateTimeFrom timestamp  with time zone not null ,
	DateTimeTo timestamp  with time zone null,
    primary key (OwnerId,VehicleId,DateTimeFrom)
);


CREATE TABLE domain.ClientEmail (
    Address varchar not null,
	ClientId uuid references  domain.Client,
	IsActive boolean not null,
    primary key (Address,ClientId)
);
 
CREATE TABLE domain.PrivateClient (
	Id uuid PRIMARY KEY references  domain.Client,
	FirstName varchar NOT NULL,
	LastName varchar,
	PersonalCode varchar 
);


CREATE TABLE domain.LegalClient (
	Id uuid PRIMARY KEY references domain.Client,
	Name varchar NOT NULL,
	RegNr varchar
);


CREATE TABLE domain.Pricing (
	Id uuid primary key, 
    SentOn timestamp with time zone ,
	PrintedOn timestamp with time zone ,
	Email varchar ,
	PartyName varchar not null,
	PartyAddress varchar,
	PartyCode varchar,
	VehicleLine1 varchar,
	VehicleLine2 varchar,
	VehicleLine3 varchar,
	VehicleLine4 varchar,
	IssuedOn timestamp with time zone not null,
	IssuerId uuid not null references domain.Employee
);

CREATE TABLE domain.Estimate (
    Id uuid primary key references domain.Pricing,
	Number varchar not null unique
);


CREATE TABLE domain.Invoice (
    Id uuid primary key references domain.Pricing,
	Number int not null unique, 
	PaymentType smallint not null,
    DueDays smallint not null,
    IsPaid boolean default false not null,
    IsCredited boolean default false NULL
);

CREATE TABLE domain.Work (
	Id uuid primary key,
	Number int not null ,
	Invoiceid uuid references domain.Invoice,
    ClientId uuid references domain.Client,
	VehicleId uuid null references domain.Vehicle,
	StartedOn timestamp with time zone not null,
	ChangedOn timestamp with time zone not null unique,
	StarterId uuid not null references domain.Employee,
	Notes varchar, 
	Odo int, 
	UserStatus varchar default 'Default' NOT NULL,
	CompletedOn timestamp with time zone, --old isdone false true
	CompleterId uuid references domain.Employee
);
 
CREATE TABLE domain.Offer
(
    Id uuid primary key,
	WorkId uuid not null references domain.Work(Id) ,
	OrderNr smallint not null,
    Notes varchar,
	EstimateId uuid references domain.Estimate,	
	IsVehileLinesOnEstimate  boolean  default false not null,
	StartedOn timestamp with time zone not null,
	StarterId uuid not null references domain.Employee,
	AcceptedOn timestamp with time zone, -- voeti vastu
	AcceptorId uuid references domain.Employee,
	unique(WorkId,OrderNr)
);

CREATE TABLE domain.RepairJob (
    Id uuid primary key,
	WorkId uuid not null references domain.Work(Id) ,
	OrderNr smallint not null,
    Notes varchar,
	StartedOn timestamp with time zone not null,
	StarterId uuid not null references domain.Employee,
	unique(WorkId,OrderNr)
);

CREATE TABLE domain.Assignment (
	WorkId uuid not null references domain.Work(Id),
	MechanicId uuid not null references domain.Employee, 
	primary key (WorkId,MechanicId)
);
 

CREATE TABLE domain.Saleable (
	Id uuid primary key, 
	Name varchar not null,
	Quantity double precision not null,
	Unit varchar not null,
	Price double precision not null,
	Discount smallint
	
);

CREATE TABLE domain.ServiceOffered (
	Id uuid primary key references domain.Saleable,
	OfferId uuid not null references domain.Offer (Id)
);

CREATE TABLE domain.ProductOffered (
	Id  uuid primary key references domain.Saleable, 
    OfferId uuid not null references domain.Offer (Id),
	Code varchar not null,
	Jnr smallint not null,
	ServiceId uuid references domain.ServiceOffered
);

CREATE TABLE domain.ServicePerformed (
	Id  uuid primary key references domain.Saleable,
	 --IsDone boolean not null default false,its done when product is instlled status
    RepairJobId uuid not null references domain.RepairJob,
	Notes varchar,
	MechanicId  uuid references domain.Employee,
	foreign key (RepairJobId)  references domain.RepairJob (Id)
);


CREATE TABLE domain.ProductInstalled (
	Id  uuid primary key references domain.Saleable,
    RepairJobId uuid not null references domain.RepairJob,
    Jnr smallint not null,
    Code varchar not null,
	Notes varchar,
	Status smallint not null, --  Tellimata, Tellitud,Kohal,Paigaldatud
	ServiceId uuid references domain.ServicePerformed,
	foreign key (RepairJobId)  references domain.RepairJob (Id)
);
 
create table domain.Storage (
  Id uuid primary key,
  Name varchar not null,
  Address varchar,
	Description varchar,
	IntroducedAt timestamp with time zone  not null
);
CREATE TABLE domain.UnitedMotorsPrice (
    Id uuid primary key,
    Price double precision not null,
    Name varchar NOT NULL,
	Address varchar
);
/*todo teenuste nimiekiri ka samale vaatele*/
CREATE TABLE domain.SparePart (
	Id uuid primary key,
    Code varchar not null,
    Name varchar not null,
    Price double precision,
    StorageId uuid null references domain.Storage,
    Quantity double precision,
    Discount smallint,
	Description varchar,
	IntroducedAt timestamp with time zone not null,
	UmPriceId uuid references domain.UnitedMotorsPrice(Id)
);
 

CREATE TABLE domain.PricingLine
(
	PricingId uuid not null references domain.Pricing,
	Nr smallint not null,
    Description varchar not null,
    Quantity double precision not null,
    UnitPrice double precision not null,
    Unit varchar not null,
    Discount smallint not null default 0,
    Total double precision not null,
    TotalWithVat double precision not null,
	primary key (PricingId, Nr)
);
 
 


CREATE INDEX idx_Vehicle_Vin ON domain.Vehicle(Vin); 
CREATE  INDEX idx_Client_Address ON domain.Client(Address); 
CREATE INDEX idx_Client_Phone ON domain.Client(Phone); 
CREATE INDEX idx_PrivateClient ON domain.PrivateClient(FirstName,LastName); 
CREATE INDEX idx_Pricing_IssuerId ON domain.Pricing(IssuerId); 
CREATE INDEX idx_work_clientid ON domain.work(clientid);
CREATE INDEX idx_work_starterid ON domain.work(starterid);
CREATE INDEX idx_work_vehicleid ON domain.work(vehicleid);
CREATE INDEX idx_offer_workid_ordernr ON domain.Offer(workid,OrderNr);
CREATE INDEX idx_repairjob_workid_ordernr ON domain.RepairJob(workid,OrderNr);
CREATE INDEX idx_offer_EstimateId ON domain.Offer(EstimateId); 
CREATE INDEX idx_Saleable_Name ON domain.Saleable(Name);  
CREATE INDEX idx_ProductOffered_Code ON domain.ProductOffered(Code);  
CREATE INDEX idx_ProductInstalled_Code ON domain.ProductInstalled(Code); 
CREATE INDEX idx_number_work_vehicleid ON domain.work(number);
CREATE INDEX idx_number_estimate_vehicleid ON domain.estimate(number);
CREATE INDEX idx_number_invoice_vehicleid ON domain.invoice(number);




CREATE TABLE public.User ( 
    UserName varchar NOT NULL unique,
    Password varchar NOT NULL,
	TenantName varchar NOT NULL,
	Email varchar NULL,
	Validated boolean NOT NULL DEFAULT false,
    Profile_Image bytea null,
    EmployeeId uuid
);
	 
CREATE SCHEMA tenant_config;

-- Create requisites table
CREATE TABLE tenant_config.requisites (
    id uuid PRIMARY KEY,
    name VARCHAR NOT NULL,
    phone VARCHAR,
    address VARCHAR,
    email VARCHAR,
    bank_account VARCHAR,
    reg_nr VARCHAR,
    tax_id VARCHAR,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Create pricing table
CREATE TABLE tenant_config.pricing (
    id uuid PRIMARY KEY,
    vat_rate INTEGER NOT NULL DEFAULT 20,
    surcharge VARCHAR,
    disclaimer VARCHAR,
    signature_line BOOLEAN NOT NULL DEFAULT true,
    invoice_email_content TEXT,
    estimate_email_content TEXT,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Add default values
INSERT INTO tenant_config.requisites (id,name, phone, address, email, bank_account, reg_nr, tax_id)
VALUES ('6dd57256-2774-424f-a61b-887bf8327329','Default Company', '+1234567890', '123 Main St', 'info@example.com', 'EE123456789012', 'REG12345', 'VAT123456');

INSERT INTO tenant_config.pricing (id,vat_rate, surcharge, disclaimer, signature_line, invoice_email_content, estimate_email_content)
VALUES (
    '3b9806b3-287b-46cc-bc17-a2d40500327b',
    20, 
    'Default Surcharge', 
    'Default Disclaimer', 
    true, 
    'Thank you for your business. Please find your invoice attached.', 
    'Thank you for your interest. Please find your estimate attached.'
);