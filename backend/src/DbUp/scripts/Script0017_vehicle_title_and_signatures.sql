-- The vehicle under the document number of estimates and invoices ("2019 Honda Civic LX"), as it was when issued.
ALTER TABLE domain.pricing ADD COLUMN IF NOT EXISTS vehicle_title varchar(200) NULL;

-- Links sent to clients to sign an estimate online, without signing in. Only the SHA-256 of the token is kept.
-- In the user list database: the link itself does not say which tenant it belongs to.
CREATE TABLE IF NOT EXISTS public.signature_link (
    token_hash char(64) PRIMARY KEY,
    tenantname varchar NOT NULL,
    company_id uuid NOT NULL,
    estimate_id uuid NOT NULL,
    created_at timestamp with time zone NOT NULL,
    expires_at timestamp with time zone NOT NULL
);
CREATE INDEX IF NOT EXISTS idx_signature_link_estimate ON public.signature_link (estimate_id);

-- The client's signature of an estimate (one per estimate; a changed offer is issued as a new estimate).
CREATE TABLE IF NOT EXISTS domain.estimate_signature (
    estimate_id uuid PRIMARY KEY,
    company_id uuid NOT NULL,
    signer_name varchar(200) NOT NULL,
    signed_at timestamp with time zone NOT NULL,
    -- PNG as a data URL (data:image/png;base64,...)
    image text NOT NULL,
    ip varchar(64) NULL
);
