-- The vehicle under the document number of estimates and invoices ("2019 Honda Civic LX"), as it was when issued.
ALTER TABLE pricing ADD COLUMN vehicle_title VARCHAR(200) NULL;

-- Links sent to clients to sign an estimate online, without signing in. Only the SHA-256 of the token is kept.
CREATE TABLE signature_link (
    token_hash CHAR(64) NOT NULL PRIMARY KEY,
    tenantname VARCHAR(255) NOT NULL,
    company_id CHAR(36) NOT NULL,
    estimate_id CHAR(36) NOT NULL,
    created_at DATETIME(6) NOT NULL,
    expires_at DATETIME(6) NOT NULL,
    INDEX idx_signature_link_estimate (estimate_id)
);

-- The client's signature of an estimate (one per estimate; a changed offer is issued as a new estimate).
CREATE TABLE estimate_signature (
    estimate_id CHAR(36) NOT NULL PRIMARY KEY,
    company_id CHAR(36) NOT NULL,
    signer_name VARCHAR(200) NOT NULL,
    signed_at DATETIME(6) NOT NULL,
    -- PNG as a data URL (data:image/png;base64,...)
    image MEDIUMTEXT NOT NULL,
    ip VARCHAR(64) NULL
);
