-- Migration: Create conversations and direct messages tables
-- Date: 2026-03-07
-- Description: Adds the persistence foundation for direct conversations and messages.

CREATE TABLE IF NOT EXISTS conversations (
    id UUID PRIMARY KEY,
    user1_id UUID NOT NULL REFERENCES users(id),
    user2_id UUID NOT NULL REFERENCES users(id),
    created_at_utc TIMESTAMPTZ NOT NULL,
    CONSTRAINT chk_conversations_distinct_users CHECK (user1_id <> user2_id)
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_conversations_user_pair
    ON conversations ((LEAST(user1_id, user2_id)), (GREATEST(user1_id, user2_id)));

CREATE TABLE IF NOT EXISTS direct_messages (
    id UUID PRIMARY KEY,
    conversation_id UUID NOT NULL REFERENCES conversations(id) ON DELETE CASCADE,
    author_user_id UUID NOT NULL REFERENCES users(id),
    content TEXT NOT NULL,
    created_at_utc TIMESTAMPTZ NOT NULL,
    updated_at_utc TIMESTAMPTZ NULL,
    deleted_at_utc TIMESTAMPTZ NULL
);

CREATE INDEX IF NOT EXISTS ix_direct_messages_conversation_id_created_at_utc
    ON direct_messages(conversation_id, created_at_utc);
