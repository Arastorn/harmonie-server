-- Migration: Create refresh_tokens table
-- Date: 2026-02-22
-- Description: Persistent refresh token storage with rotation support

CREATE TABLE IF NOT EXISTS refresh_tokens (
    id UUID PRIMARY KEY,
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    token_hash VARCHAR(128) NOT NULL UNIQUE,
    created_at_utc TIMESTAMP NOT NULL DEFAULT (NOW() AT TIME ZONE 'UTC'),
    expires_at_utc TIMESTAMP NOT NULL,
    revoked_at_utc TIMESTAMP
);

-- Indexes for lookup and cleanup
CREATE INDEX idx_refresh_tokens_user_id ON refresh_tokens(user_id);
CREATE INDEX idx_refresh_tokens_expires_at_utc ON refresh_tokens(expires_at_utc);
CREATE INDEX idx_refresh_tokens_active_user ON refresh_tokens(user_id) WHERE revoked_at_utc IS NULL;

-- Comments for documentation
COMMENT ON TABLE refresh_tokens IS 'Persistent refresh tokens used for session continuation';
COMMENT ON COLUMN refresh_tokens.token_hash IS 'SHA-256 hash of the refresh token (never store plain token)';
COMMENT ON COLUMN refresh_tokens.expires_at_utc IS 'Absolute UTC expiration date for the refresh token';
COMMENT ON COLUMN refresh_tokens.revoked_at_utc IS 'UTC revocation date set when token is rotated or invalidated';
