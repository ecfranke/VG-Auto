-- Several companies in one database. Every business row belongs to a company; users see only the
-- data of their own company. Existing data becomes the first company
-- (00000000-0000-0000-0000-000000000001), which is also the default for rows written without one.
CREATE TABLE company (
    id CHAR(36) NOT NULL PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    created_at DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6)
);
INSERT INTO company (id, name)
SELECT '00000000-0000-0000-0000-000000000001', COALESCE((SELECT name FROM tenant_config_requisites LIMIT 1), 'Company');

ALTER TABLE employee ADD COLUMN company_id CHAR(36) NOT NULL DEFAULT '00000000-0000-0000-0000-000000000001', ADD INDEX idx_employee_company (company_id), ADD CONSTRAINT fk_employee_company FOREIGN KEY (company_id) REFERENCES company (id);
ALTER TABLE client ADD COLUMN company_id CHAR(36) NOT NULL DEFAULT '00000000-0000-0000-0000-000000000001', ADD INDEX idx_client_company (company_id), ADD CONSTRAINT fk_client_company FOREIGN KEY (company_id) REFERENCES company (id);
ALTER TABLE vehicle ADD COLUMN company_id CHAR(36) NOT NULL DEFAULT '00000000-0000-0000-0000-000000000001', ADD INDEX idx_vehicle_company (company_id), ADD CONSTRAINT fk_vehicle_company FOREIGN KEY (company_id) REFERENCES company (id);
ALTER TABLE work ADD COLUMN company_id CHAR(36) NOT NULL DEFAULT '00000000-0000-0000-0000-000000000001', ADD CONSTRAINT fk_work_company FOREIGN KEY (company_id) REFERENCES company (id);
ALTER TABLE pricing ADD COLUMN company_id CHAR(36) NOT NULL DEFAULT '00000000-0000-0000-0000-000000000001', ADD INDEX idx_pricing_company (company_id), ADD CONSTRAINT fk_pricing_company FOREIGN KEY (company_id) REFERENCES company (id);
ALTER TABLE invoice ADD COLUMN company_id CHAR(36) NOT NULL DEFAULT '00000000-0000-0000-0000-000000000001', ADD CONSTRAINT fk_invoice_company FOREIGN KEY (company_id) REFERENCES company (id);
ALTER TABLE estimate ADD COLUMN company_id CHAR(36) NOT NULL DEFAULT '00000000-0000-0000-0000-000000000001', ADD CONSTRAINT fk_estimate_company FOREIGN KEY (company_id) REFERENCES company (id);
ALTER TABLE storage ADD COLUMN company_id CHAR(36) NOT NULL DEFAULT '00000000-0000-0000-0000-000000000001', ADD INDEX idx_storage_company (company_id), ADD CONSTRAINT fk_storage_company FOREIGN KEY (company_id) REFERENCES company (id);
ALTER TABLE sparepart ADD COLUMN company_id CHAR(36) NOT NULL DEFAULT '00000000-0000-0000-0000-000000000001', ADD INDEX idx_sparepart_company (company_id), ADD CONSTRAINT fk_sparepart_company FOREIGN KEY (company_id) REFERENCES company (id);
ALTER TABLE unitedmotorsprice ADD COLUMN company_id CHAR(36) NOT NULL DEFAULT '00000000-0000-0000-0000-000000000001', ADD CONSTRAINT fk_umprice_company FOREIGN KEY (company_id) REFERENCES company (id);
ALTER TABLE tenant_config_requisites ADD COLUMN company_id CHAR(36) NOT NULL DEFAULT '00000000-0000-0000-0000-000000000001', ADD UNIQUE INDEX uq_requisites_company (company_id), ADD CONSTRAINT fk_requisites_company FOREIGN KEY (company_id) REFERENCES company (id);
ALTER TABLE tenant_config_pricing ADD COLUMN company_id CHAR(36) NOT NULL DEFAULT '00000000-0000-0000-0000-000000000001', ADD UNIQUE INDEX uq_pricing_config_company (company_id), ADD CONSTRAINT fk_pricing_config_company FOREIGN KEY (company_id) REFERENCES company (id);
ALTER TABLE app_user ADD COLUMN company_id CHAR(36) NOT NULL DEFAULT '00000000-0000-0000-0000-000000000001';

-- work, invoice and estimate numbers are counted per company
ALTER TABLE work DROP INDEX work_number_key, ADD UNIQUE INDEX uq_work_company_number (company_id, number);
ALTER TABLE invoice DROP INDEX number, ADD UNIQUE INDEX uq_invoice_company_number (company_id, number);
ALTER TABLE estimate DROP INDEX number, ADD UNIQUE INDEX uq_estimate_company_number (company_id, number);
