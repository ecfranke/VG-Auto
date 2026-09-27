-- Roles for the user administration (/admin): user, admin, superadmin.
-- The owner (initial administrator) is marked by Script0007.
ALTER TABLE app_user
    ADD COLUMN role VARCHAR(20) NOT NULL DEFAULT 'user',
    ADD COLUMN is_owner BOOLEAN NOT NULL DEFAULT FALSE,
    ADD COLUMN disabled BOOLEAN NOT NULL DEFAULT FALSE;

-- What administrators did (accounts, roles, passwords, company settings).
CREATE TABLE admin_audit_log (
    id BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    created_at DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    tenantname VARCHAR(255) NOT NULL,
    actor VARCHAR(255) NOT NULL,
    action VARCHAR(50) NOT NULL,
    target VARCHAR(255) NULL,
    details TEXT NULL,
    INDEX idx_admin_audit_log_created (tenantname, created_at)
);
