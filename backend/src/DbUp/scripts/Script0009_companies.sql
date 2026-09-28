-- Several companies in one database. Every business row belongs to a company; users see only the
-- data of their own company. Existing data becomes the first company
-- (00000000-0000-0000-0000-000000000001), which is also the default for rows written without one.
CREATE TABLE IF NOT EXISTS domain.company (
    id uuid PRIMARY KEY,
    name varchar(255) NOT NULL,
    created_at timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP
);
INSERT INTO domain.company (id, name)
SELECT '00000000-0000-0000-0000-000000000001', COALESCE((SELECT name FROM tenant_config.requisites LIMIT 1), 'Company')
WHERE NOT EXISTS (SELECT 1 FROM domain.company);

ALTER TABLE domain.employee ADD COLUMN IF NOT EXISTS company_id uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000001' REFERENCES domain.company;
ALTER TABLE domain.client ADD COLUMN IF NOT EXISTS company_id uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000001' REFERENCES domain.company;
ALTER TABLE domain.vehicle ADD COLUMN IF NOT EXISTS company_id uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000001' REFERENCES domain.company;
ALTER TABLE domain.work ADD COLUMN IF NOT EXISTS company_id uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000001' REFERENCES domain.company;
ALTER TABLE domain.pricing ADD COLUMN IF NOT EXISTS company_id uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000001' REFERENCES domain.company;
ALTER TABLE domain.invoice ADD COLUMN IF NOT EXISTS company_id uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000001' REFERENCES domain.company;
ALTER TABLE domain.estimate ADD COLUMN IF NOT EXISTS company_id uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000001' REFERENCES domain.company;
ALTER TABLE domain.storage ADD COLUMN IF NOT EXISTS company_id uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000001' REFERENCES domain.company;
ALTER TABLE domain.sparepart ADD COLUMN IF NOT EXISTS company_id uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000001' REFERENCES domain.company;
ALTER TABLE domain.unitedmotorsprice ADD COLUMN IF NOT EXISTS company_id uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000001' REFERENCES domain.company;
ALTER TABLE tenant_config.requisites ADD COLUMN IF NOT EXISTS company_id uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000001' REFERENCES domain.company;
ALTER TABLE tenant_config.pricing ADD COLUMN IF NOT EXISTS company_id uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000001' REFERENCES domain.company;
ALTER TABLE public.user ADD COLUMN IF NOT EXISTS company_id uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000001';

CREATE INDEX IF NOT EXISTS idx_employee_company ON domain.employee (company_id);
CREATE INDEX IF NOT EXISTS idx_client_company ON domain.client (company_id);
CREATE INDEX IF NOT EXISTS idx_vehicle_company ON domain.vehicle (company_id);
CREATE INDEX IF NOT EXISTS idx_pricing_company ON domain.pricing (company_id);
CREATE INDEX IF NOT EXISTS idx_storage_company ON domain.storage (company_id);
CREATE INDEX IF NOT EXISTS idx_sparepart_company ON domain.sparepart (company_id);
CREATE UNIQUE INDEX IF NOT EXISTS uq_requisites_company ON tenant_config.requisites (company_id);
CREATE UNIQUE INDEX IF NOT EXISTS uq_pricing_config_company ON tenant_config.pricing (company_id);

-- work, invoice and estimate numbers are counted per company
ALTER TABLE domain.work DROP CONSTRAINT IF EXISTS work_number_key;
ALTER TABLE domain.invoice DROP CONSTRAINT IF EXISTS invoice_number_key;
ALTER TABLE domain.estimate DROP CONSTRAINT IF EXISTS estimate_number_key;
CREATE UNIQUE INDEX IF NOT EXISTS uq_work_company_number ON domain.work (company_id, number);
CREATE UNIQUE INDEX IF NOT EXISTS uq_invoice_company_number ON domain.invoice (company_id, number);
CREATE UNIQUE INDEX IF NOT EXISTS uq_estimate_company_number ON domain.estimate (company_id, number);
