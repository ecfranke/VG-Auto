-- Vehicle details on invoices and estimates, per company: whether "show vehicle information" is on when a
-- document is issued, and which details are printed (flags: 1 year/make/model, 2 license plate, 4 odometer, 8 VIN).
ALTER TABLE tenant_config_pricing ADD COLUMN invoice_show_vehicle TINYINT(1) NOT NULL DEFAULT 0;
ALTER TABLE tenant_config_pricing ADD COLUMN invoice_vehicle_fields INT NOT NULL DEFAULT 15;
ALTER TABLE tenant_config_pricing ADD COLUMN estimate_show_vehicle TINYINT(1) NOT NULL DEFAULT 0;
ALTER TABLE tenant_config_pricing ADD COLUMN estimate_vehicle_fields INT NOT NULL DEFAULT 15;
