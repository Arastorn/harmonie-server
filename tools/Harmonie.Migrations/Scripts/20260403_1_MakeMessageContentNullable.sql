-- Migration: Make message content nullable
-- Date: 2026-04-03
-- Description: Allow NULL content for attachment-only messages

ALTER TABLE messages ALTER COLUMN content DROP NOT NULL;
