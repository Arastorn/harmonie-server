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
CREATE INDEX IF NOT EXISTS idx_refresh_tokens_user_id ON refresh_tokens(user_id);
CREATE INDEX IF NOT EXISTS idx_refresh_tokens_expires_at_utc ON refresh_tokens(expires_at_utc);
CREATE INDEX IF NOT EXISTS idx_refresh_tokens_cleanup ON refresh_tokens(expires_at_utc) WHERE revoked_at_utc IS NOT NULL;

-- Comments for documentation
COMMENT ON TABLE refresh_tokens IS 'Persistent refresh tokens used for session continuation';
COMMENT ON COLUMN refresh_tokens.token_hash IS 'SHA-256 hash of the refresh token (never store plain token)';
COMMENT ON COLUMN refresh_tokens.expires_at_utc IS 'Absolute UTC expiration date for the refresh token';
COMMENT ON COLUMN refresh_tokens.revoked_at_utc IS 'UTC revocation date set when token is rotated or invalidated';

-- Cleanup function for expired/revoked tokens older than 12 hours
CREATE OR REPLACE FUNCTION cleanup_refresh_tokens()
RETURNS void
LANGUAGE sql
AS $$
    DELETE FROM refresh_tokens
    WHERE revoked_at_utc IS NOT NULL
      AND expires_at_utc < (NOW() AT TIME ZONE 'UTC') - INTERVAL '12 hours';
$$;

-- Try to install pg_cron when available; ignore if unavailable on this PostgreSQL instance
DO $$
BEGIN
    BEGIN
        EXECUTE 'CREATE EXTENSION IF NOT EXISTS pg_cron';
    EXCEPTION
        WHEN OTHERS THEN
            RAISE NOTICE 'pg_cron extension not available, skipping SQL scheduler setup';
    END;
END
$$;

-- Schedule hourly cleanup with pg_cron when extension is available
DO $$
BEGIN
    BEGIN
        IF EXISTS (SELECT 1 FROM pg_extension WHERE extname = 'pg_cron') THEN
            IF NOT EXISTS (SELECT 1 FROM cron.job WHERE jobname = 'cleanup_refresh_tokens_hourly') THEN
                PERFORM cron.schedule(
                    'cleanup_refresh_tokens_hourly',
                    '5 * * * *',
                    'SELECT cleanup_refresh_tokens();');
            END IF;
        END IF;
    EXCEPTION
        WHEN OTHERS THEN
            RAISE NOTICE 'pg_cron scheduling unavailable, skipping refresh token cleanup job setup';
    END;
END
$$;
