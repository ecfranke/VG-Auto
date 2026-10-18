-- Vehicle details on invoices and estimates, per company: whether "show vehicle information" is on when a
-- document is issued, and which details are printed (flags: 1 year/make/model, 2 license plate, 4 odometer, 8 VIN).
ALTER TABLE tenant_config.pricing ADD COLUMN IF NOT EXISTS invoice_show_vehicle boolean NOT NULL DEFAULT false;
ALTER TABLE tenant_config.pricing ADD COLUMN IF NOT EXISTS invoice_vehicle_fields integer NOT NULL DEFAULT 15;
ALTER TABLE tenant_config.pricing ADD COLUMN IF NOT EXISTS estimate_show_vehicle boolean NOT NULL DEFAULT false;
ALTER TABLE tenant_config.pricing ADD COLUMN IF NOT EXISTS estimate_vehicle_fields integer NOT NULL DEFAULT 15;
