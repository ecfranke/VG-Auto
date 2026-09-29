-- Email transport of each company: its own SMTP, Microsoft 365 (Graph) or Gmail account, or the built-in (system)
-- transport when a super administrator allows it. Passwords and client secrets are stored encrypted.
CREATE TABLE tenant_config_email (
    company_id CHAR(36) NOT NULL PRIMARY KEY,
    provider VARCHAR(20) NOT NULL DEFAULT 'system',
    system_allowed TINYINT(1) NOT NULL DEFAULT 0,
    from_address VARCHAR(255) NULL,
    from_name VARCHAR(255) NULL,
    smtp_host VARCHAR(255) NULL,
    smtp_port INT NULL,
    smtp_user VARCHAR(255) NULL,
    smtp_password VARCHAR(1000) NULL,
    smtp_security VARCHAR(20) NULL,
    graph_tenant_id VARCHAR(255) NULL,
    graph_client_id VARCHAR(255) NULL,
    graph_client_secret VARCHAR(1000) NULL,
    graph_sender VARCHAR(255) NULL,
    updated_at DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    CONSTRAINT fk_email_company FOREIGN KEY (company_id) REFERENCES company (id)
);
-- existing companies keep sending through the server's transport
INSERT INTO tenant_config_email (company_id, provider, system_allowed)
SELECT c.id, 'system', 1 FROM company c;

-- The built-in transport set by a super administrator (one row). Without it the Email section of the server
-- configuration is used.
CREATE TABLE system_email (
    id INT NOT NULL PRIMARY KEY,
    provider VARCHAR(20) NOT NULL,
    from_address VARCHAR(255) NULL,
    from_name VARCHAR(255) NULL,
    smtp_host VARCHAR(255) NULL,
    smtp_port INT NULL,
    smtp_user VARCHAR(255) NULL,
    smtp_password VARCHAR(1000) NULL,
    smtp_security VARCHAR(20) NULL,
    graph_tenant_id VARCHAR(255) NULL,
    graph_client_id VARCHAR(255) NULL,
    graph_client_secret VARCHAR(1000) NULL,
    graph_sender VARCHAR(255) NULL,
    updated_at DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6)
);

-- company administrators see the audit entries of their own company
ALTER TABLE admin_audit_log ADD COLUMN company_id CHAR(36) NULL, ADD INDEX idx_admin_audit_log_company (tenantname, company_id, created_at);
