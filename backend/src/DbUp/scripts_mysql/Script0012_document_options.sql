-- Whether the company's bank account and Reg No are printed on estimates and invoices (off by default).
ALTER TABLE tenant_config_pricing ADD COLUMN show_bank_account TINYINT(1) NOT NULL DEFAULT 0;
ALTER TABLE tenant_config_pricing ADD COLUMN show_reg_no TINYINT(1) NOT NULL DEFAULT 0;
