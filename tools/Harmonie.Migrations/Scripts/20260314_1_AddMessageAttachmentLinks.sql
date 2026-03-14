-- Migration: Add message attachment links
-- Date: 2026-03-14
-- Description: Links messages to uploaded_files so uploaded attachments can be referenced by channel and conversation messages.

CREATE TABLE IF NOT EXISTS message_attachments (
    message_id UUID NOT NULL REFERENCES messages(id) ON DELETE CASCADE,
    uploaded_file_id UUID NOT NULL REFERENCES uploaded_files(id) ON DELETE CASCADE,
    position INT NOT NULL,
    CONSTRAINT pk_message_attachments PRIMARY KEY (message_id, uploaded_file_id),
    CONSTRAINT uq_message_attachments_position UNIQUE (message_id, position),
    CONSTRAINT chk_message_attachments_position_non_negative CHECK (position >= 0)
);

CREATE INDEX IF NOT EXISTS ix_message_attachments_message_position
    ON message_attachments(message_id, position);

CREATE INDEX IF NOT EXISTS ix_message_attachments_uploaded_file_id
    ON message_attachments(uploaded_file_id);

COMMENT ON TABLE message_attachments IS 'Ordered attachment links between messages and uploaded files';
