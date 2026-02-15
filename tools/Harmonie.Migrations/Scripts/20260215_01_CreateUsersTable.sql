-- Migration: Create users table
-- Date: 2026-02-15
-- Description: Initial user table with all authentication fields

CREATE TABLE IF NOT EXISTS users (
    id UUID PRIMARY KEY,
    email VARCHAR(320) NOT NULL UNIQUE,
    username VARCHAR(32) NOT NULL UNIQUE,
    password_hash VARCHAR(256) NOT NULL,
    avatar_url VARCHAR(2048),
    is_email_verified BOOLEAN NOT NULL DEFAULT FALSE,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    display_name VARCHAR(100),
    bio VARCHAR(500),
    created_at_utc TIMESTAMP NOT NULL DEFAULT (NOW() AT TIME ZONE 'UTC'),
    updated_at_utc TIMESTAMP,
    last_login_at_utc TIMESTAMP,
    deleted_at TIMESTAMP
);

-- Indexes for performance
CREATE INDEX idx_users_email ON users(email) WHERE deleted_at IS NULL;
CREATE INDEX idx_users_username ON users(username) WHERE deleted_at IS NULL;
CREATE INDEX idx_users_active ON users(is_active) WHERE deleted_at IS NULL;

-- Comments for documentation
COMMENT ON TABLE users IS 'User accounts across all Harmonie instances';
COMMENT ON COLUMN users.id IS 'Unique identifier (UUID v4)';
COMMENT ON COLUMN users.email IS 'Email address for authentication';
COMMENT ON COLUMN users.username IS 'Display username (unique per instance)';
COMMENT ON COLUMN users.password_hash IS 'BCrypt/PBKDF2 hashed password';
COMMENT ON COLUMN users.deleted_at IS 'Soft delete timestamp';
