-- Whether the company's bank account and Reg No are printed on estimates and invoices (off by default).
ALTER TABLE tenant_config.pricing ADD COLUMN IF NOT EXISTS show_bank_account boolean NOT NULL DEFAULT false;
ALTER TABLE tenant_config.pricing ADD COLUMN IF NOT EXISTS show_reg_no boolean NOT NULL DEFAULT false;
