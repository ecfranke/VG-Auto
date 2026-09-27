-- Company currency (chosen by an administrator in the settings) and the currency of each issued
-- document. Documents issued before this change were printed in euros, so they keep EUR.
ALTER TABLE tenant_config.pricing ADD COLUMN IF NOT EXISTS currency varchar(3) NOT NULL DEFAULT 'CAD';
ALTER TABLE domain.pricing ADD COLUMN IF NOT EXISTS currency varchar(3) NULL;
UPDATE domain.pricing SET currency = 'EUR' WHERE currency IS NULL;
