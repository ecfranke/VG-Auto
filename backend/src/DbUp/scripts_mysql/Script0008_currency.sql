-- Company currency (chosen by an administrator in the settings) and the currency of each issued
-- document. Documents issued before this change were printed in euros, so they keep EUR.
ALTER TABLE tenant_config_pricing ADD COLUMN currency VARCHAR(3) NOT NULL DEFAULT 'CAD';
ALTER TABLE pricing ADD COLUMN currency VARCHAR(3) NULL;
UPDATE pricing SET currency = 'EUR' WHERE currency IS NULL;
