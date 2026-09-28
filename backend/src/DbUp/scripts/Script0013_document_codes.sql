-- Estimates and invoices are named like the work (RP_TF_2019_HC_2026_09_28_15), fixed when they are issued.
-- Existing documents get their code in Script0014.
ALTER TABLE domain.pricing ADD COLUMN IF NOT EXISTS code varchar(100) NULL;
