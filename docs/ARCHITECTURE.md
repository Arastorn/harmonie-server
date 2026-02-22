# 🏗️ Harmonie Architecture Overview

## 📊 Project Structure

```
Harmonie/
├── src/                                    # Source code
│   ├── Harmonie.Domain/                   # ❤️ CORE - Business Logic
│   │   ├── Common/                        # Base classes & interfaces
│   │   │   ├── Entity.cs                 # Base entity with domain events
│   │   │   ├── IDomainEvent.cs          # Domain event interface
│   │   │   ├── Result.cs                # Result pattern for operations
│   │   │   └── DomainException.cs       # Base domain exception
│   │   ├── Entities/                     # Domain entities
│   │   │   └── User.cs                  # User aggregate root
│   │   ├── ValueObjects/                 # Strongly-typed primitives
│   │   │   ├── UserId.cs                # User identifier
│   │   │   ├── Email.cs                 # Email with validation
│   │   │   └── Username.cs              # Username with validation
│   │   ├── Events/                       # Domain events
│   │   │   └── UserEvents.cs            # User-related events
│   │   └── Exceptions/                   # Domain exceptions
│   │       └── UserExceptions.cs        # User-specific exceptions
│   │
│   ├── Harmonie.Application/             # 📋 USE CASES (CQRS)
│   │   ├── Commands/                     # Write operations
│   │   │   └── Users/
│   │   │       ├── Register/            # User registration
│   │   │       │   ├── RegisterUserCommand.cs
│   │   │       │   ├── RegisterUserCommandHandler.cs
│   │   │       │   ├── RegisterUserCommandValidator.cs
│   │   │       │   └── RegisterUserResponse.cs
│   │   │       ├── Login/               # User login
│   │   │       │   ├── LoginCommand.cs
│   │   │       │   ├── LoginCommandHandler.cs
│   │   │       │   ├── LoginCommandValidator.cs
│   │   │       │   └── LoginResponse.cs
│   │   │       └── RefreshToken/        # Token refresh (partial)
│   │   ├── Queries/                      # Read operations (future)
│   │   ├── Interfaces/                   # Ports (Hexagonal)
│   │   │   ├── IUserRepository.cs       # User data access port
│   │   │   ├── IPasswordHasher.cs       # Password hashing port
│   │   │   └── IJwtTokenService.cs      # JWT generation port
│   │   ├── ValidationBehavior.cs         # MediatR pipeline
│   │   └── DependencyInjection.cs        # Service registration
│   │
│   ├── Harmonie.Infrastructure/          # 🔧 ADAPTERS
│   │   ├── Persistence/                  # Data access
│   │   │   └── UserRepository.cs        # Dapper implementation
│   │   ├── Authentication/               # Security
│   │   │   ├── JwtTokenService.cs       # JWT implementation
│   │   │   └── PasswordHasher.cs        # Password hashing
│   │   ├── Configuration/                # Settings
│   │   │   ├── JwtSettings.cs
│   │   │   └── DatabaseSettings.cs
│   │   └── DependencyInjection.cs        # Service registration
│   │
│   └── Harmonie.API/                     # 🌐 WEB LAYER
│       ├── Controllers/                   # REST endpoints
│       │   └── AuthController.cs         # /api/auth/* endpoints
│       ├── Middleware/                    # HTTP pipeline
│       │   └── GlobalExceptionHandler.cs # Error handling
│       ├── Program.cs                     # Application entry point
│       └── appsettings.json              # Configuration
│
├── tests/                                 # Test projects
│   ├── Harmonie.Domain.Tests/            # Domain unit tests
│   │   ├── UserTests.cs
│   │   ├── EmailTests.cs
│   │   └── UsernameTests.cs
│   ├── Harmonie.Application.Tests/       # Application tests
│   └── Harmonie.API.IntegrationTests/    # API integration tests
│
├── tools/                                 # Utilities
│   └── Harmonie.Migrations/              # Database migrations
│       ├── Program.cs                    # Migration runner (DbUp)
│       └── Scripts/
│           └── 0001_CreateUsersTable.sql # Initial schema
│
├── .github/workflows/                     # CI/CD
│   └── ci.yml                            # Build, test, deploy
│
├── docker-compose.yml                     # Local development
├── Dockerfile                             # Production container
├── Harmonie.sln                          # Solution file
├── README.md                             # Project overview
├── agent.md                              # AI assistant context
├── GETTING_STARTED.md                    # Quick start guide
├── CONTRIBUTING.md                       # Contribution guidelines
└── LICENSE                               # AGPL-3.0

```

## 🎯 Layer Dependencies

```
┌─────────────────────────────────────────┐
│          API Layer                      │  ← HTTP, Controllers, Middleware
│          (Harmonie.API)                 │     Depends on: Application
└─────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────┐
│      Application Layer (CQRS)           │  ← Commands, Queries, Handlers
│      (Harmonie.Application)             │     Depends on: Domain
│      • Commands (Write)                 │     Defines: Interfaces (Ports)
│      • Queries (Read)                   │
│      • Interfaces (Ports)               │
└─────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────┐
│      Domain Layer (Core)                │  ← Entities, Value Objects
│      (Harmonie.Domain)                  │     Depends on: NOTHING
│      • Entities                         │     Pure business logic
│      • Value Objects                    │
│      • Domain Events                    │
└─────────────────────────────────────────┘
                    ↑
┌─────────────────────────────────────────┐
│   Infrastructure Layer (Adapters)       │  ← Database, JWT, External
│   (Harmonie.Infrastructure)             │     Depends on: Application, Domain
│   • Persistence (Dapper)                │     Implements: Ports
│   • Authentication (JWT)                │
│   • External Services                   │
└─────────────────────────────────────────┘
```

## 📦 NuGet Packages Used

### Domain Layer
- **NONE** (Pure .NET, no dependencies)

### Application Layer
- `FluentValidation` - Input validation

### Infrastructure Layer
- `Dapper` - Micro-ORM for PostgreSQL
- `Npgsql` - PostgreSQL driver
- `System.IdentityModel.Tokens.Jwt` - JWT tokens
- `Microsoft.AspNetCore.Identity` - Password hashing

### API Layer
- `Swashbuckle.AspNetCore` - Swagger/OpenAPI
- `Serilog.AspNetCore` - Structured logging

### Tests
- `xUnit` - Testing framework
- `FluentAssertions` - Fluent test assertions
- `Moq` - Mocking framework

## 🔐 Authentication Flow

```
1. User Registration
   ┌─────────┐
   │ Client  │
   └────┬────┘
        │ POST /api/auth/register
        │ { email, username, password }
        ↓
   ┌────────────────────────────────┐
   │  RegisterUserCommandHandler    │
   │  1. Validate input             │
   │  2. Check duplicates           │
   │  3. Hash password              │
   │  4. Create User entity         │
   │  5. Save to database           │
   │  6. Generate JWT tokens        │
   └────────┬───────────────────────┘
            │
            ↓
   ┌─────────────────────┐
   │  201 Created        │
   │  {                  │
   │    userId,          │
   │    accessToken,     │
   │    refreshToken     │
   │  }                  │
   └─────────────────────┘

2. User Login
   ┌─────────┐
   │ Client  │
   └────┬────┘
        │ POST /api/auth/login
        │ { emailOrUsername, password }
        ↓
   ┌────────────────────────────────┐
   │  LoginCommandHandler           │
   │  1. Find user by email/username│
   │  2. Verify password            │
   │  3. Update last login          │
   │  4. Generate JWT tokens        │
   └────────┬───────────────────────┘
            │
            ↓
   ┌─────────────────────┐
   │  200 OK             │
   │  {                  │
   │    accessToken,     │
   │    refreshToken     │
   │  }                  │
   └─────────────────────┘

3. Access Protected Endpoint
   ┌─────────┐
   │ Client  │
   └────┬────┘
        │ GET /api/guilds
        │ Authorization: Bearer {accessToken}
        ↓
   ┌────────────────────────────────┐
   │  JWT Middleware                │
   │  1. Validate token signature   │
   │  2. Check expiration           │
   │  3. Extract user claims        │
   │  4. Set HttpContext.User       │
   └────────┬───────────────────────┘
            │
            ↓
   ┌─────────────────────┐
   │  Protected Resource │
   └─────────────────────┘
```

## 🗄️ Database Schema

```sql
users
├── id                    UUID PRIMARY KEY
├── email                 VARCHAR(320) UNIQUE NOT NULL
├── username              VARCHAR(32) UNIQUE NOT NULL
├── password_hash         VARCHAR(256) NOT NULL
├── avatar_url            VARCHAR(2048)
├── is_email_verified     BOOLEAN DEFAULT FALSE
├── is_active             BOOLEAN DEFAULT TRUE
├── display_name          VARCHAR(100)
├── bio                   VARCHAR(500)
├── created_at_utc        TIMESTAMP NOT NULL
├── updated_at_utc        TIMESTAMP
├── last_login_at_utc     TIMESTAMP
└── deleted_at            TIMESTAMP (soft delete)

Indexes:
- idx_users_email (WHERE deleted_at IS NULL)
- idx_users_username (WHERE deleted_at IS NULL)
- idx_users_active (WHERE deleted_at IS NULL)
```

## 🚀 Getting Started Commands

```bash
# 1. Clone & setup
git clone https://github.com/harmonie-chat/harmonie.git
cd harmonie

# 2. Start PostgreSQL
docker-compose up -d postgres

# 3. Run migrations
cd tools/Harmonie.Migrations && dotnet run

# 4. Run API
cd ../../src/Harmonie.API && dotnet run

# 5. Open Swagger
open https://localhost:7001/swagger

# 6. Run tests
dotnet test
```

## 📊 Current Status

### ✅ Completed (MVP Sprint 1 - Auth)
- [x] Clean Architecture setup
- [x] Domain entities (User)
- [x] Value objects (Email, Username, UserId)
- [x] CQRS with MediatR
- [x] User registration
- [x] User login
- [x] JWT authentication
- [x] Password hashing
- [x] FluentValidation
- [x] Dapper repository
- [x] PostgreSQL schema
- [x] Database migrations (DbUp)
- [x] Unit tests (Domain)
- [x] API endpoints
- [x] Swagger documentation
- [x] Docker Compose
- [x] CI/CD workflow
- [x] Documentation

### 🚧 Next Sprint - Guilds & Channels
- [ ] Guild aggregate
- [ ] Channel entity
- [ ] Member entity (User-Guild relation)
- [ ] Permission system
- [ ] Role entity
- [ ] Guild CRUD operations
- [ ] Channel CRUD operations
- [ ] Member management

### 🔮 Future Features
- [ ] SignalR real-time messaging
- [ ] Message entity & persistence
- [ ] File upload/storage
- [ ] User presence (online/offline)
- [ ] Typing indicators
- [ ] Voice channels (LiveKit)
- [ ] Video calls
- [ ] Screen sharing
- [ ] Client application
- [ ] Multi-server federation

## 🎓 Key Design Decisions

1. **Clean Architecture**: Domain at center, independent of infrastructure
2. **CQRS with MediatR**: Separate read/write operations
3. **Dapper over EF Core**: Performance and SQL control
4. **JWT Tokens**: Stateless authentication
5. **Value Objects**: Prevent primitive obsession
6. **Result Pattern**: Functional error handling
7. **Domain Events**: Loosely coupled side effects
8. **DbUp Migrations**: SQL-first, version controlled
9. **Strongly-Typed IDs**: Type safety (UserId, not Guid)
10. **AGPL-3.0 License**: Copyleft for network services

## 📚 Resources

- [Clean Architecture](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [CQRS Pattern](https://martinfowler.com/bliki/CQRS.html)
- [Domain-Driven Design](https://www.domainlanguage.com/ddd/)
- [MediatR Documentation](https://github.com/jbogard/MediatR/wiki)
- [Dapper Documentation](https://github.com/DapperLib/Dapper)

---

**Project Statistics**:
- **Lines of Code**: ~3,000
- **Test Coverage**: TBD (run `dotnet test /p:CollectCoverage=true`)
- **Project Files**: 56
- **Layers**: 4 (API, Application, Domain, Infrastructure)

**Made with 🎵 for free, open communication**
