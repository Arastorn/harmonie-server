-- Migration: Add soft delete support to channel_messages
-- Date: 2026-03-01
-- Description: Adds deleted_at_utc column for soft-deleting messages

ALTER TABLE channel_messages
    ADD COLUMN IF NOT EXISTS deleted_at_utc TIMESTAMPTZ NULL;

COMMENT ON COLUMN channel_messages.deleted_at_utc IS 'Timestamp when the message was soft-deleted; NULL means the message is active';
