-- Roles for the user administration (/admin): user, admin, superadmin.
-- The owner (initial administrator) is marked by Script0007.
ALTER TABLE public.user ADD COLUMN IF NOT EXISTS role varchar(20) NOT NULL DEFAULT 'user';
ALTER TABLE public.user ADD COLUMN IF NOT EXISTS is_owner boolean NOT NULL DEFAULT false;
ALTER TABLE public.user ADD COLUMN IF NOT EXISTS disabled boolean NOT NULL DEFAULT false;

-- What administrators did (accounts, roles, passwords, company settings).
CREATE TABLE IF NOT EXISTS public.admin_audit_log (
    id bigserial PRIMARY KEY,
    created_at timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    tenantname varchar(255) NOT NULL,
    actor varchar(255) NOT NULL,
    action varchar(50) NOT NULL,
    target varchar(255) NULL,
    details text NULL
);
CREATE INDEX IF NOT EXISTS idx_admin_audit_log_created ON public.admin_audit_log (tenantname, created_at DESC);
