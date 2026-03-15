CREATE TABLE guild_bans (
    guild_id       UUID NOT NULL REFERENCES guilds(id) ON DELETE CASCADE,
    user_id        UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    reason         VARCHAR(512) NULL,
    banned_by      UUID NOT NULL REFERENCES users(id),
    created_at_utc TIMESTAMPTZ NOT NULL,
    PRIMARY KEY (guild_id, user_id)
);

CREATE INDEX ix_guild_bans_user_id ON guild_bans (user_id);
