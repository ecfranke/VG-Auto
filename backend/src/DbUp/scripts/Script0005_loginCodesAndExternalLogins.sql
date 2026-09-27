-- One time email codes (login verification, password reset, linking an external account)
CREATE TABLE IF NOT EXISTS public.auth_challenge (
    id uuid PRIMARY KEY,
    purpose varchar(20) NOT NULL,
    tenantname varchar NOT NULL,
    employeeid uuid NOT NULL,
    code_hash varchar(128) NOT NULL,
    payload varchar(500) NULL,
    attempts integer NOT NULL DEFAULT 0,
    sends integer NOT NULL DEFAULT 1,
    created_at timestamp with time zone NOT NULL,
    expires_at timestamp with time zone NOT NULL,
    consumed_at timestamp with time zone NULL
);
CREATE INDEX IF NOT EXISTS idx_auth_challenge_expires ON public.auth_challenge (expires_at);

-- Accounts of external identity providers (Microsoft) linked to a user
CREATE TABLE IF NOT EXISTS public.user_external_login (
    provider varchar(50) NOT NULL,
    subject varchar(200) NOT NULL,
    tenantname varchar NOT NULL,
    employeeid uuid NOT NULL,
    email varchar NULL,
    created_at timestamp with time zone NOT NULL,
    PRIMARY KEY (provider, subject)
);
CREATE INDEX IF NOT EXISTS idx_user_external_login_user ON public.user_external_login (tenantname, employeeid);
