-- Walk-in customer kept only on the work (not saved as a client and vehicle): name, phone and the vehicle typed in.
ALTER TABLE domain.work ADD COLUMN IF NOT EXISTS walkin_name varchar(200);
ALTER TABLE domain.work ADD COLUMN IF NOT EXISTS walkin_phone varchar(50);
ALTER TABLE domain.work ADD COLUMN IF NOT EXISTS walkin_manufacturer varchar(100);
ALTER TABLE domain.work ADD COLUMN IF NOT EXISTS walkin_model varchar(100);
ALTER TABLE domain.work ADD COLUMN IF NOT EXISTS walkin_year integer;
ALTER TABLE domain.work ADD COLUMN IF NOT EXISTS walkin_licenseplate varchar(50);
ALTER TABLE domain.work ADD COLUMN IF NOT EXISTS walkin_vin varchar(50);
