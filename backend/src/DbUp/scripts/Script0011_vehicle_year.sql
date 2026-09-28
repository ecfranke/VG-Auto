-- Model year of a vehicle (optional), shown as "2019 Honda Civic".
ALTER TABLE domain.vehicle ADD COLUMN IF NOT EXISTS year smallint NULL;
