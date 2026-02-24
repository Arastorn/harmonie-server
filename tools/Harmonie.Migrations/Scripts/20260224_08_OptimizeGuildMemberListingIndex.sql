-- Migration: Optimize guild member listing index
-- Date: 2026-02-24
-- Description: Add index aligned with guild member list query order.

CREATE INDEX IF NOT EXISTS idx_guild_members_guild_joined_user
    ON guild_members(guild_id, joined_at_utc ASC, user_id ASC)
    INCLUDE (role);
