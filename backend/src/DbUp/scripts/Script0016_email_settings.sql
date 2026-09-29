-- Email transport of each company: its own SMTP, Microsoft 365 (Graph) or Gmail account, or the built-in (system)
-- transport when a super administrator allows it. Passwords and client secrets are stored encrypted.
CREATE TABLE IF NOT EXISTS tenant_config.email (
    company_id uuid PRIMARY KEY REFERENCES domain.company,
    provider varchar(20) NOT NULL DEFAULT 'system',
    system_allowed boolean NOT NULL DEFAULT false,
    from_address varchar(255) NULL,
    from_name varchar(255) NULL,
    smtp_host varchar(255) NULL,
    smtp_port integer NULL,
    smtp_user varchar(255) NULL,
    smtp_password varchar(1000) NULL,
    smtp_security varchar(20) NULL,
    graph_tenant_id varchar(255) NULL,
    graph_client_id varchar(255) NULL,
    graph_client_secret varchar(1000) NULL,
    graph_sender varchar(255) NULL,
    updated_at timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP
);
-- existing companies keep sending through the server's transport
INSERT INTO tenant_config.email (company_id, provider, system_allowed)
SELECT c.id, 'system', true FROM domain.company c
WHERE NOT EXISTS (SELECT 1 FROM tenant_config.email e WHERE e.company_id = c.id);

-- The built-in transport set by a super administrator (one row). Without it the Email section of the server
-- configuration is used.
CREATE TABLE IF NOT EXISTS public.system_email (
    id integer PRIMARY KEY,
    provider varchar(20) NOT NULL,
    from_address varchar(255) NULL,
    from_name varchar(255) NULL,
    smtp_host varchar(255) NULL,
    smtp_port integer NULL,
    smtp_user varchar(255) NULL,
    smtp_password varchar(1000) NULL,
    smtp_security varchar(20) NULL,
    graph_tenant_id varchar(255) NULL,
    graph_client_id varchar(255) NULL,
    graph_client_secret varchar(1000) NULL,
    graph_sender varchar(255) NULL,
    updated_at timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- company administrators see the audit entries of their own company
ALTER TABLE public.admin_audit_log ADD COLUMN IF NOT EXISTS company_id uuid NULL;
CREATE INDEX IF NOT EXISTS idx_admin_audit_log_company ON public.admin_audit_log (tenantname, company_id, created_at DESC);
