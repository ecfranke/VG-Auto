-- Taxes by the place of registration of the company (country and province/state), up to two taxes
-- (for example GST + PST in British Columbia, GST + QST in Quebec, HST in Ontario).
-- Prices are entered before tax from now on; the taxes are added to the subtotal.
ALTER TABLE tenant_config_pricing ADD COLUMN tax_country VARCHAR(2) NULL;
ALTER TABLE tenant_config_pricing ADD COLUMN tax_region VARCHAR(10) NULL;
ALTER TABLE tenant_config_pricing ADD COLUMN tax1_name VARCHAR(30) NULL;
ALTER TABLE tenant_config_pricing ADD COLUMN tax1_rate DECIMAL(7,4) NOT NULL DEFAULT 0;
ALTER TABLE tenant_config_pricing ADD COLUMN tax2_name VARCHAR(30) NULL;
ALTER TABLE tenant_config_pricing ADD COLUMN tax2_rate DECIMAL(7,4) NOT NULL DEFAULT 0;
UPDATE tenant_config_pricing
   SET tax1_rate = vat_rate,
       tax1_name = CASE WHEN currency = 'CAD' THEN 'GST' ELSE 'VAT' END,
       tax_country = CASE WHEN currency = 'CAD' THEN 'CA' ELSE NULL END
 WHERE tax1_name IS NULL;

-- the taxes of an issued document (null: issued before this change, prices included VAT)
ALTER TABLE pricing ADD COLUMN tax1_name VARCHAR(30) NULL;
ALTER TABLE pricing ADD COLUMN tax1_rate DECIMAL(7,4) NULL;
ALTER TABLE pricing ADD COLUMN tax2_name VARCHAR(30) NULL;
ALTER TABLE pricing ADD COLUMN tax2_rate DECIMAL(7,4) NULL;
