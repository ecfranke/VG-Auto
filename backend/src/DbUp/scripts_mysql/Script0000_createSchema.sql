-- MySQL 8.0+ schema. Mirrors the PostgreSQL scripts 0000, 0002, 0003 and 0005:
--   domain.<table>          -> <table>
--   tenant_config.<table>   -> tenant_config_<table>
--   public.user             -> app_user
-- Ids are CHAR(36) GUIDs, timestamps are DATETIME(6) in UTC.

CREATE TABLE employee (
    id CHAR(36) NOT NULL PRIMARY KEY,
    firstname VARCHAR(255) NOT NULL,
    lastname VARCHAR(255) NOT NULL,
    email VARCHAR(255) NULL,
    phone VARCHAR(100) NULL,
    address VARCHAR(500) NULL,
    proffession VARCHAR(255) NULL,
    description TEXT NULL,
    introducedat DATETIME(6) NOT NULL
);

CREATE TABLE vehicle (
    id CHAR(36) NOT NULL PRIMARY KEY,
    producer VARCHAR(255) NULL,
    model VARCHAR(255) NULL,
    regnr VARCHAR(100) NOT NULL,
    vin VARCHAR(100) NULL,
    odo INT NULL,
    body VARCHAR(255) NULL,
    drivingside VARCHAR(50) NULL,
    engine VARCHAR(255) NULL,
    productiondate DATE NULL,
    region VARCHAR(255) NULL,
    series VARCHAR(255) NULL,
    transmission VARCHAR(255) NULL,
    description TEXT NULL,
    introducedat DATETIME(6) NOT NULL,
    INDEX idx_vehicle_vin (vin)
);

CREATE TABLE client (
    id CHAR(36) NOT NULL PRIMARY KEY,
    address VARCHAR(500) NULL,
    country VARCHAR(255) NULL,
    region VARCHAR(255) NULL,
    city VARCHAR(255) NULL,
    postalcode VARCHAR(50) NULL,
    phone VARCHAR(100) NULL,
    description TEXT NULL,
    isasshole BOOLEAN NOT NULL DEFAULT FALSE,
    introducedat DATETIME(6) NOT NULL,
    INDEX idx_client_address (address),
    INDEX idx_client_phone (phone)
);

CREATE TABLE vehicleregistration (
    ownerid CHAR(36) NOT NULL,
    vehicleid CHAR(36) NOT NULL,
    datetimefrom DATETIME(6) NOT NULL,
    datetimeto DATETIME(6) NULL,
    PRIMARY KEY (ownerid, vehicleid, datetimefrom),
    CONSTRAINT fk_vehicleregistration_owner FOREIGN KEY (ownerid) REFERENCES client (id),
    CONSTRAINT fk_vehicleregistration_vehicle FOREIGN KEY (vehicleid) REFERENCES vehicle (id)
);

CREATE TABLE clientemail (
    address VARCHAR(255) NOT NULL,
    clientid CHAR(36) NOT NULL,
    isactive BOOLEAN NOT NULL,
    PRIMARY KEY (address, clientid),
    CONSTRAINT fk_clientemail_client FOREIGN KEY (clientid) REFERENCES client (id)
);

CREATE TABLE privateclient (
    id CHAR(36) NOT NULL PRIMARY KEY,
    firstname VARCHAR(255) NOT NULL,
    lastname VARCHAR(255) NULL,
    personalcode VARCHAR(100) NULL,
    INDEX idx_privateclient (firstname, lastname),
    CONSTRAINT fk_privateclient_client FOREIGN KEY (id) REFERENCES client (id)
);

CREATE TABLE legalclient (
    id CHAR(36) NOT NULL PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    regnr VARCHAR(100) NULL,
    CONSTRAINT fk_legalclient_client FOREIGN KEY (id) REFERENCES client (id)
);

CREATE TABLE pricing (
    id CHAR(36) NOT NULL PRIMARY KEY,
    senton DATETIME(6) NULL,
    printedon DATETIME(6) NULL,
    email VARCHAR(255) NULL,
    partyname VARCHAR(500) NOT NULL,
    partyaddress VARCHAR(500) NULL,
    partycode VARCHAR(100) NULL,
    vehicleline1 VARCHAR(500) NULL,
    vehicleline2 VARCHAR(500) NULL,
    vehicleline3 VARCHAR(500) NULL,
    vehicleline4 VARCHAR(500) NULL,
    issuedon DATETIME(6) NOT NULL,
    issuerid CHAR(36) NOT NULL,
    INDEX idx_pricing_issuerid (issuerid),
    CONSTRAINT fk_pricing_issuer FOREIGN KEY (issuerid) REFERENCES employee (id)
);

CREATE TABLE estimate (
    id CHAR(36) NOT NULL PRIMARY KEY,
    number VARCHAR(100) NOT NULL UNIQUE,
    CONSTRAINT fk_estimate_pricing FOREIGN KEY (id) REFERENCES pricing (id)
);

CREATE TABLE invoice (
    id CHAR(36) NOT NULL PRIMARY KEY,
    number INT NOT NULL UNIQUE,
    paymenttype SMALLINT NOT NULL,
    duedays SMALLINT NOT NULL,
    ispaid BOOLEAN NOT NULL DEFAULT FALSE,
    iscredited BOOLEAN NULL DEFAULT FALSE,
    CONSTRAINT fk_invoice_pricing FOREIGN KEY (id) REFERENCES pricing (id)
);

CREATE TABLE work (
    id CHAR(36) NOT NULL PRIMARY KEY,
    number INT NOT NULL,
    invoiceid CHAR(36) NULL,
    clientid CHAR(36) NULL,
    vehicleid CHAR(36) NULL,
    startedon DATETIME(6) NOT NULL,
    changedon DATETIME(6) NOT NULL UNIQUE,
    starterid CHAR(36) NOT NULL,
    notes TEXT NULL,
    odo INT NULL,
    userstatus VARCHAR(50) NOT NULL DEFAULT 'Default',
    completedon DATETIME(6) NULL,
    completerid CHAR(36) NULL,
    CONSTRAINT work_number_key UNIQUE (number),
    INDEX idx_work_clientid (clientid),
    INDEX idx_work_starterid (starterid),
    INDEX idx_work_vehicleid (vehicleid),
    CONSTRAINT fk_work_invoice FOREIGN KEY (invoiceid) REFERENCES invoice (id),
    CONSTRAINT fk_work_client FOREIGN KEY (clientid) REFERENCES client (id),
    CONSTRAINT fk_work_vehicle FOREIGN KEY (vehicleid) REFERENCES vehicle (id),
    CONSTRAINT fk_work_starter FOREIGN KEY (starterid) REFERENCES employee (id),
    CONSTRAINT fk_work_completer FOREIGN KEY (completerid) REFERENCES employee (id)
);

CREATE TABLE offer (
    id CHAR(36) NOT NULL PRIMARY KEY,
    workid CHAR(36) NOT NULL,
    ordernr SMALLINT NOT NULL,
    notes TEXT NULL,
    estimateid CHAR(36) NULL,
    isvehilelinesonestimate BOOLEAN NOT NULL DEFAULT FALSE,
    startedon DATETIME(6) NOT NULL,
    starterid CHAR(36) NOT NULL,
    acceptedon DATETIME(6) NULL,
    acceptorid CHAR(36) NULL,
    UNIQUE KEY uq_offer_work_ordernr (workid, ordernr),
    INDEX idx_offer_estimateid (estimateid),
    CONSTRAINT fk_offer_work FOREIGN KEY (workid) REFERENCES work (id),
    CONSTRAINT fk_offer_estimate FOREIGN KEY (estimateid) REFERENCES estimate (id),
    CONSTRAINT fk_offer_starter FOREIGN KEY (starterid) REFERENCES employee (id),
    CONSTRAINT fk_offer_acceptor FOREIGN KEY (acceptorid) REFERENCES employee (id)
);

CREATE TABLE repairjob (
    id CHAR(36) NOT NULL PRIMARY KEY,
    workid CHAR(36) NOT NULL,
    ordernr SMALLINT NOT NULL,
    notes TEXT NULL,
    startedon DATETIME(6) NOT NULL,
    starterid CHAR(36) NOT NULL,
    UNIQUE KEY uq_repairjob_work_ordernr (workid, ordernr),
    CONSTRAINT fk_repairjob_work FOREIGN KEY (workid) REFERENCES work (id),
    CONSTRAINT fk_repairjob_starter FOREIGN KEY (starterid) REFERENCES employee (id)
);

CREATE TABLE assignment (
    workid CHAR(36) NOT NULL,
    mechanicid CHAR(36) NOT NULL,
    PRIMARY KEY (workid, mechanicid),
    CONSTRAINT fk_assignment_work FOREIGN KEY (workid) REFERENCES work (id),
    CONSTRAINT fk_assignment_mechanic FOREIGN KEY (mechanicid) REFERENCES employee (id)
);

CREATE TABLE saleable (
    id CHAR(36) NOT NULL PRIMARY KEY,
    name VARCHAR(500) NOT NULL,
    quantity DOUBLE NOT NULL,
    unit VARCHAR(50) NOT NULL,
    price DOUBLE NOT NULL,
    discount SMALLINT NULL,
    INDEX idx_saleable_name (name)
);

CREATE TABLE serviceoffered (
    id CHAR(36) NOT NULL PRIMARY KEY,
    offerid CHAR(36) NOT NULL,
    CONSTRAINT fk_serviceoffered_saleable FOREIGN KEY (id) REFERENCES saleable (id),
    CONSTRAINT fk_serviceoffered_offer FOREIGN KEY (offerid) REFERENCES offer (id)
);

CREATE TABLE productoffered (
    id CHAR(36) NOT NULL PRIMARY KEY,
    offerid CHAR(36) NOT NULL,
    code VARCHAR(255) NOT NULL,
    jnr SMALLINT NOT NULL,
    serviceid CHAR(36) NULL,
    INDEX idx_productoffered_code (code),
    CONSTRAINT fk_productoffered_saleable FOREIGN KEY (id) REFERENCES saleable (id),
    CONSTRAINT fk_productoffered_offer FOREIGN KEY (offerid) REFERENCES offer (id),
    CONSTRAINT fk_productoffered_service FOREIGN KEY (serviceid) REFERENCES serviceoffered (id)
);

CREATE TABLE serviceperformed (
    id CHAR(36) NOT NULL PRIMARY KEY,
    repairjobid CHAR(36) NOT NULL,
    notes TEXT NULL,
    mechanicid CHAR(36) NULL,
    CONSTRAINT fk_serviceperformed_saleable FOREIGN KEY (id) REFERENCES saleable (id),
    CONSTRAINT fk_serviceperformed_job FOREIGN KEY (repairjobid) REFERENCES repairjob (id),
    CONSTRAINT fk_serviceperformed_mechanic FOREIGN KEY (mechanicid) REFERENCES employee (id)
);

CREATE TABLE productinstalled (
    id CHAR(36) NOT NULL PRIMARY KEY,
    repairjobid CHAR(36) NOT NULL,
    jnr SMALLINT NOT NULL,
    code VARCHAR(255) NOT NULL,
    notes TEXT NULL,
    status SMALLINT NOT NULL,
    serviceid CHAR(36) NULL,
    INDEX idx_productinstalled_code (code),
    CONSTRAINT fk_productinstalled_saleable FOREIGN KEY (id) REFERENCES saleable (id),
    CONSTRAINT fk_productinstalled_job FOREIGN KEY (repairjobid) REFERENCES repairjob (id),
    CONSTRAINT fk_productinstalled_service FOREIGN KEY (serviceid) REFERENCES serviceperformed (id)
);

CREATE TABLE storage (
    id CHAR(36) NOT NULL PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    address VARCHAR(500) NULL,
    description TEXT NULL,
    introducedat DATETIME(6) NOT NULL
);

CREATE TABLE unitedmotorsprice (
    id CHAR(36) NOT NULL PRIMARY KEY,
    price DOUBLE NOT NULL,
    name VARCHAR(500) NOT NULL,
    address VARCHAR(500) NULL
);

CREATE TABLE sparepart (
    id CHAR(36) NOT NULL PRIMARY KEY,
    code VARCHAR(255) NOT NULL,
    name VARCHAR(500) NOT NULL,
    price DOUBLE NULL,
    storageid CHAR(36) NULL,
    quantity DOUBLE NULL,
    discount SMALLINT NULL,
    description TEXT NULL,
    introducedat DATETIME(6) NOT NULL,
    umpriceid CHAR(36) NULL,
    CONSTRAINT fk_sparepart_storage FOREIGN KEY (storageid) REFERENCES storage (id),
    CONSTRAINT fk_sparepart_umprice FOREIGN KEY (umpriceid) REFERENCES unitedmotorsprice (id)
);

CREATE TABLE pricingline (
    pricingid CHAR(36) NOT NULL,
    nr SMALLINT NOT NULL,
    description TEXT NOT NULL,
    quantity DOUBLE NOT NULL,
    unitprice DOUBLE NOT NULL,
    unit VARCHAR(50) NOT NULL,
    discount SMALLINT NOT NULL DEFAULT 0,
    total DOUBLE NOT NULL,
    totalwithvat DOUBLE NOT NULL,
    PRIMARY KEY (pricingid, nr),
    CONSTRAINT fk_pricingline_pricing FOREIGN KEY (pricingid) REFERENCES pricing (id)
);

CREATE TABLE app_user (
    username VARCHAR(255) NOT NULL UNIQUE,
    password VARCHAR(255) NOT NULL,
    tenantname VARCHAR(255) NOT NULL,
    email VARCHAR(255) NULL,
    validated BOOLEAN NOT NULL DEFAULT FALSE,
    profile_image LONGBLOB NULL,
    employeeid CHAR(36) NULL,
    must_change_password BOOLEAN NOT NULL DEFAULT FALSE,
    failed_login_count INT NOT NULL DEFAULT 0,
    locked_until DATETIME(6) NULL,
    INDEX idx_app_user_email (email)
);

CREATE TABLE tenant_config_requisites (
    id CHAR(36) NOT NULL PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    phone VARCHAR(100) NULL,
    address VARCHAR(500) NULL,
    email VARCHAR(255) NULL,
    bank_account VARCHAR(100) NULL,
    reg_nr VARCHAR(100) NULL,
    tax_id VARCHAR(100) NULL,
    created_at DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    updated_at DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6)
);

CREATE TABLE tenant_config_pricing (
    id CHAR(36) NOT NULL PRIMARY KEY,
    vat_rate INT NOT NULL DEFAULT 20,
    surcharge VARCHAR(500) NULL,
    disclaimer TEXT NULL,
    signature_line BOOLEAN NOT NULL DEFAULT TRUE,
    invoice_email_content TEXT NULL,
    estimate_email_content TEXT NULL,
    created_at DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    updated_at DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6)
);

INSERT INTO tenant_config_requisites (id, name, phone, address, email, bank_account, reg_nr, tax_id)
VALUES ('6dd57256-2774-424f-a61b-887bf8327329', 'Default Company', '+1234567890', '123 Main St', 'info@example.com', 'EE123456789012', 'REG12345', 'VAT123456');

INSERT INTO tenant_config_pricing (id, vat_rate, surcharge, disclaimer, signature_line, invoice_email_content, estimate_email_content)
VALUES ('3b9806b3-287b-46cc-bc17-a2d40500327b', 20, 'Default Surcharge', 'Default Disclaimer', TRUE,
        'Thank you for your business. Please find your invoice attached.',
        'Thank you for your interest. Please find your estimate attached.');

CREATE TABLE auth_challenge (
    id CHAR(36) NOT NULL PRIMARY KEY,
    purpose VARCHAR(20) NOT NULL,
    tenantname VARCHAR(255) NOT NULL,
    employeeid CHAR(36) NOT NULL,
    code_hash VARCHAR(128) NOT NULL,
    payload VARCHAR(500) NULL,
    attempts INT NOT NULL DEFAULT 0,
    sends INT NOT NULL DEFAULT 1,
    created_at DATETIME(6) NOT NULL,
    expires_at DATETIME(6) NOT NULL,
    consumed_at DATETIME(6) NULL,
    INDEX idx_auth_challenge_expires (expires_at)
);

CREATE TABLE user_external_login (
    provider VARCHAR(50) NOT NULL,
    subject VARCHAR(200) NOT NULL,
    tenantname VARCHAR(255) NOT NULL,
    employeeid CHAR(36) NOT NULL,
    email VARCHAR(255) NULL,
    created_at DATETIME(6) NOT NULL,
    PRIMARY KEY (provider, subject),
    INDEX idx_user_external_login_user (tenantname, employeeid)
);
