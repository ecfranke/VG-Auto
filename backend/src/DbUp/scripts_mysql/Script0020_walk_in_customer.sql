-- Walk-in customer kept only on the work (not saved as a client and vehicle): name, phone and the vehicle typed in.
ALTER TABLE work
  ADD COLUMN walkin_name VARCHAR(200) NULL,
  ADD COLUMN walkin_phone VARCHAR(50) NULL,
  ADD COLUMN walkin_manufacturer VARCHAR(100) NULL,
  ADD COLUMN walkin_model VARCHAR(100) NULL,
  ADD COLUMN walkin_year INT NULL,
  ADD COLUMN walkin_licenseplate VARCHAR(50) NULL,
  ADD COLUMN walkin_vin VARCHAR(50) NULL;
