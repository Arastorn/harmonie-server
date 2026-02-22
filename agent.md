# Agent Context - Harmonie Server

Purpose: concise implementation context for AI coding assistants working in this repository.

## Project Snapshot

Harmonie Server is a .NET 10 backend for a self-hosted communication platform.

Current implemented scope:
- Auth: register and login
- JWT access token generation
- Refresh token generation (not persisted yet)
- Health endpoint
- PostgreSQL persistence for users

## Source Layout

- `src/Harmonie.API`: startup, middleware, endpoint mapping
- `src/Harmonie.Application`: vertical slices and interfaces
- `src/Harmonie.Domain`: entities, value objects, domain rules
- `src/Harmonie.Infrastructure`: Dapper repository, JWT, password hashing
- `tools/Harmonie.Migrations`: DbUp migration runner + SQL scripts
- `tests/*`: domain, application, and API integration tests

## Active Endpoints

- `GET /health`
- `POST /api/auth/register`
- `POST /api/auth/login`

## Architecture Rules

- Keep Domain independent of framework/infrastructure concerns.
- Keep Application feature-first (`Features/{Domain}/{Feature}`).
- Prefer explicit flow (endpoint -> validator -> handler -> interfaces).
- Infrastructure implements `IUserRepository`, `IPasswordHasher`, `IJwtTokenService`.

## Coding Guidelines

- Use English for code, comments, docs, and commit messages.
- Prefer clear and explicit code over abstraction-heavy patterns.
- Validate at boundaries (FluentValidation + domain invariants).
- Use async APIs end-to-end for I/O operations.
- Keep SQL parameterized (Dapper command parameters only).

## Feature Addition Checklist

1. Create a new folder under `src/Harmonie.Application/Features`.
2. Add request/validator/handler/response/endpoint files.
3. Register handler in `src/Harmonie.Application/DependencyInjection.cs`.
4. Map endpoint in `src/Harmonie.API/Program.cs`.
5. Add or update tests.
6. Update docs if behavior changed.

## Testing

- Run all tests: `dotnet test`
- Integration tests use `WebApplicationFactory<Program>`.

## Known Gaps

- Refresh token persistence, rotation, and revocation are TODO.
- No guild/channel/message endpoints yet.

## Reference Docs

- `README.md`
- `docs/GETTING_STARTED.md`
- `docs/ARCHITECTURE.md`
- `docs/VERTICAL_SLICE_ARCHITECTURE.md`

Last updated: 2026-02-22
