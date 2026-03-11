-- Migration: Add purpose column to uploaded_files
-- Date: 2026-03-11
-- Description: Adds upload purpose metadata to support access control.
--              Existing rows are backfilled as 'attachment'.

ALTER TABLE uploaded_files
    ADD COLUMN IF NOT EXISTS purpose VARCHAR(50) NOT NULL DEFAULT 'attachment';

-- Remove the default after backfill so future inserts must provide a value explicitly
ALTER TABLE uploaded_files
    ALTER COLUMN purpose DROP DEFAULT;

COMMENT ON COLUMN uploaded_files.purpose IS 'Upload purpose: attachment, avatar, guild_icon';
