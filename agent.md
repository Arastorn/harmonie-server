# Agent Context - Harmonie Project 🎵

> **Purpose**: This file provides context for AI assistants (Claude, Copilot, etc.) working on the Harmonie codebase.

## 📋 Project Overview

**Harmonie** is an open-source, self-hosted communication platform (Discord alternative).

**Key Characteristics:**
- Each Harmonie server instance is fully independent (self-hosted)
- Clients can connect to multiple server instances simultaneously
- Users can participate in multiple guilds across different servers
- No central server - fully decentralized architecture
- Focus on privacy, ownership, and freedom

## 🏗️ Architecture Decisions

### Architecture Style: Clean Architecture + Vertical Slice

**Why this choice?**
- ✅ Domain-centric: Business logic is independent of infrastructure
- ✅ Testable: Easy to mock dependencies via interfaces (ports)
- ✅ Scalable: Features are isolated and can be developed independently
- ✅ Multi-instance friendly: Each instance is self-contained
- ✅ Maintainable: Clear separation of concerns
- ✅ No unnecessary abstractions: Direct, explicit code flow
- ✅ No external dependencies: Pure .NET, no MediatR licensing issues

### What is Vertical Slice Architecture?

Instead of organizing code by technical layers (Controllers, Services, Repositories), we organize by **features** (Register, Login, CreateGuild). Each feature contains everything it needs:

```
Features/
└── Auth/
    ├── Register/
    │   ├── RegisterEndpoint.cs      # Minimal API endpoint
    │   ├── RegisterRequest.cs       # Input DTO
    │   ├── RegisterResponse.cs      # Output DTO  
    │   ├── RegisterValidator.cs     # FluentValidation
    │   └── RegisterHandler.cs       # Business logic
    └── Login/
        └── ... (same structure)
```

**Benefits:**
- Adding a feature doesn't affect others
- Easy to find all related code
- Simple to test
- No "magic" - code is explicit and easy to follow

### Layer Responsibilities

```
┌─────────────────────────────────────────────────────────────┐
│  API Layer (Harmonie.API)                                    │
│  - Minimal API Endpoints: HTTP request/response             │
│  - Middleware: Auth, error handling, logging                 │
│  - Endpoint registration in Program.cs                       │
│  - Dependency: Application layer only                        │
└─────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────┐
│  Application Layer (Vertical Slice)                          │
│  - Features/: Organized by feature, not layer                │
│    - Each feature has: Endpoint, Handler, Request,           │
│      Response, Validator                                     │
│  - Handlers: Business logic orchestration                    │
│  - Interfaces: Ports for infrastructure (IUserRepository)    │
│  - Dependency: Domain layer only                             │
└─────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────┐
│  Domain Layer (Core)                                         │
│  - Entities: User, Guild, Channel, Message                   │
│  - Value Objects: Email, UserId, ChannelId                   │
│  - Domain Events: UserCreated, MessageSent                   │
│  - Domain Exceptions: UserNotFoundException                  │
│  - NO DEPENDENCIES on other layers                           │
└─────────────────────────────────────────────────────────────┘
                              ↑
┌─────────────────────────────────────────────────────────────┐
│  Infrastructure Layer (Adapters)                             │
│  - Persistence: Dapper repositories implementing ports       │
│  - Authentication: JWT token generation/validation           │
│  - External: Email, file storage (future)                    │
│  - Implements: Application layer interfaces                  │
└─────────────────────────────────────────────────────────────┘
```

### Technology Stack

| Layer | Technology | Reason |
|-------|-----------|--------|
| **API** | ASP.NET Core 10 Minimal API | Modern, fast, less boilerplate |
| **Architecture** | Vertical Slice | Simple, maintainable, no unnecessary abstraction |
| **Real-time** | SignalR | Native .NET, WebSocket support |
| **Database** | PostgreSQL 18.2 | Open-source, robust, JSON support |
| **ORM** | Dapper | Lightweight, fast, control over SQL |
| **Auth** | JWT + ASP.NET Core Identity | Industry standard, stateless |
| **DI** | Built-in .NET DI | Native, sufficient for our needs |
| **Testing** | xUnit + FluentAssertions | Standard .NET testing stack |
| **Logging** | Serilog | Structured logging, multiple sinks |
| **Validation** | FluentValidation | Declarative, testable validation |

### Database Strategy

**Dapper over Entity Framework Core - Why?**
- ✅ Performance: Direct SQL control, no query translation overhead
- ✅ Simplicity: No complex change tracking or migrations issues
- ✅ Read/Write optimization: CQRS queries can be highly optimized
- ✅ Learning: Forces developers to understand SQL
- ⚠️ Trade-off: More manual mapping, no automatic migrations

**Migration Strategy:**
- DbUp for version-controlled SQL scripts
- One migration = one SQL file
- Migrations run at application startup (dev) or via CLI (prod)

## 🎯 Coding Conventions

### General Principles

1. **English everywhere**: Code, comments, docs, commit messages
2. **Explicit over implicit**: Favor clarity over cleverness
3. **Fail fast**: Validate at boundaries, throw meaningful exceptions
4. **Immutability**: Prefer readonly, init properties where possible
5. **Async all the way**: No sync-over-async anti-patterns

### Naming Conventions

```csharp
// ✅ Good
public class UserRepository : IUserRepository
public async Task<User?> GetByIdAsync(UserId id)
public record CreateUserCommand(string Email, string Username);
public sealed class UserCreatedEvent : IDomainEvent

// ❌ Bad
public class UserRepo  // Too abbreviated
public User GetById(string id)  // Not async, primitive type
public class CreateUser  // Missing Command suffix
```

### File Organization

```
Harmonie.Application/
├── Features/
│   └── Auth/
│       ├── Register/
│       │   ├── RegisterEndpoint.cs         # Maps HTTP endpoint
│       │   ├── RegisterRequest.cs          # Input DTO
│       │   ├── RegisterResponse.cs         # Output DTO
│       │   ├── RegisterValidator.cs        # FluentValidation
│       │   └── RegisterHandler.cs          # Business logic
│       └── Login/
│           └── ... (same structure)
├── Common/
│   ├── IEndpoint.cs                        # Optional marker interface
│   └── EndpointExtensions.cs               # Helper methods
└── Interfaces/
    └── IUserRepository.cs                   # Ports
```

**Rule**: One feature = one folder with all related files

**How to add a new feature:**
1. Create `Features/{Domain}/{FeatureName}` folder
2. Add Request, Response, Validator, Handler, Endpoint
3. Register handler in DependencyInjection.cs
4. Map endpoint in Program.cs

### Domain Entities

```csharp
// ✅ Good: Rich domain model with behavior
public sealed class User
{
    public UserId Id { get; private set; }
    public Email Email { get; private set; }
    public string Username { get; private set; }
    private readonly List<DomainEvent> _domainEvents = [];

    // Factory method
    public static User Create(Email email, string username, string hashedPassword)
    {
        var user = new User
        {
            Id = UserId.New(),
            Email = email,
            Username = username,
            // ...
        };
        
        user.AddDomainEvent(new UserCreatedEvent(user.Id));
        return user;
    }

    // Business logic
    public void UpdateEmail(Email newEmail)
    {
        if (Email == newEmail) return;
        
        Email = newEmail;
        AddDomainEvent(new UserEmailChangedEvent(Id, newEmail));
    }
}

// ❌ Bad: Anemic model (just getters/setters)
public class User
{
    public Guid Id { get; set; }
    public string Email { get; set; }
    public string Username { get; set; }
}
```

### Value Objects

```csharp
// Use C# 9+ records for value objects
public sealed record Email
{
    public string Value { get; }

    private Email(string value) => Value = value;

    public static Result<Email> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result<Email>.Failure("Email cannot be empty");
        
        if (!IsValidEmail(value))
            return Result<Email>.Failure("Invalid email format");
        
        return Result<Email>.Success(new Email(value.ToLowerInvariant()));
    }
}
```

### Error Handling

```csharp
// ✅ Use Result<T> pattern for domain operations
public sealed record Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string? Error { get; }
    
    public static Result<T> Success(T value) => new(true, value, null);
    public static Result<T> Failure(string error) => new(false, default, error);
}

// ✅ Use domain exceptions for business rule violations
public sealed class UserNotFoundException : DomainException
{
    public UserNotFoundException(UserId userId) 
        : base($"User with ID '{userId}' was not found") { }
}

// ❌ Don't use exceptions for flow control
public User? GetUser(UserId id)
{
    try 
    {
        return _repository.GetById(id);
    }
    catch 
    {
        return null; // Bad: silent failure
    }
}
```

### Dependency Injection

```csharp
// ✅ Register by layer in Program.cs
builder.Services.AddApplication();  // Extension method in Application layer
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddPresentation();

// ✅ In Application layer
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        // ...
        return services;
    }
}
```

### Testing Strategy

**Unit Tests** (fast, isolated)
```csharp
// Test domain logic and handlers
[Fact]
public void User_Create_ShouldRaiseDomainEvent()
{
    // Arrange
    var email = Email.Create("test@harmonie.chat").Value;
    
    // Act
    var user = User.Create(email, "testuser", "hashedpass");
    
    // Assert
    user.DomainEvents.Should().ContainSingle()
        .Which.Should().BeOfType<UserCreatedEvent>();
}
```

**Integration Tests** (slower, database)
```csharp
// Test API endpoints with real database (Testcontainers)
public class AuthControllerTests : IClassFixture<WebApplicationFactory>
{
    [Fact]
    public async Task Register_ValidRequest_ReturnsCreated()
    {
        // Arrange
        var request = new RegisterRequest("test@harmonie.chat", "testuser", "password123");
        
        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }
}
```

## 🔐 Security Best Practices

### JWT Implementation

```csharp
// ✅ Store refresh tokens in database with expiration
// ✅ Short-lived access tokens (15 min)
// ✅ Long-lived refresh tokens (30 days)
// ✅ Rotate refresh tokens on each use
// ✅ Revocation support (blacklist table)
```

### Password Hashing

```csharp
// ✅ Use ASP.NET Core Identity PasswordHasher
// ✅ PBKDF2 with 10,000+ iterations
// ❌ Never store plain passwords
// ❌ Never log passwords
```

### Input Validation

```csharp
// ✅ Validate at API boundary (FluentValidation)
// ✅ Validate in domain layer (business rules)
// ✅ Sanitize user input (XSS prevention)
// ❌ Trust client-side validation only
```

## 🚀 Performance Guidelines

### Database Queries

```csharp
// ✅ Use async/await consistently
public async Task<User?> GetByIdAsync(UserId id, CancellationToken ct = default)
{
    const string sql = "SELECT * FROM users WHERE id = @Id";
    return await _connection.QueryFirstOrDefaultAsync<User>(sql, new { Id = id.Value }, ct);
}

// ✅ Use parameterized queries (Dapper does this)
// ❌ Never concatenate SQL strings
```

### Caching Strategy (Future)

- Redis for distributed cache (multi-instance)
- Cache frequently read data (user profiles, guild metadata)
- Cache-aside pattern
- Short TTL for real-time data

### SignalR Optimization (Future)

- Use strongly-typed hubs
- Backplane with Redis for multi-instance
- Message batching for high-frequency updates

## 📝 Git Workflow

### Commit Messages

```
feat: add user registration endpoint
fix: resolve JWT token expiration issue
docs: update API documentation
refactor: extract validation logic to FluentValidation
test: add integration tests for auth flow
chore: update dependencies
```

**Format**: `<type>: <description>`

**Types**: feat, fix, docs, style, refactor, test, chore

### Branch Strategy

- `main` - production-ready code
- `develop` - integration branch
- `feature/*` - new features
- `fix/*` - bug fixes
- `release/*` - release preparation

## 🔄 Development Workflow

1. **Create feature branch**: `git checkout -b feature/user-authentication`
2. **Write failing test**: TDD approach when possible
3. **Implement feature**: Domain → Application → API
4. **Ensure tests pass**: `dotnet test`
5. **Update documentation**: README, agent.md, API docs
6. **Create PR**: Detailed description, link to issue
7. **Code review**: At least one approval required
8. **Merge**: Squash commits for clean history

## 🐛 Debugging Tips

### Enable detailed logging

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

### Common Issues

**Issue**: Database connection fails
**Solution**: Check PostgreSQL is running, verify connection string

**Issue**: JWT validation fails
**Solution**: Ensure clock sync between servers, check secret key

**Issue**: Migration fails
**Solution**: Check SQL syntax, ensure idempotent migrations

## 📚 Learning Resources

### Architecture
- [Clean Architecture by Uncle Bob](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [Hexagonal Architecture](https://alistair.cockburn.us/hexagonal-architecture/)
- [CQRS by Martin Fowler](https://martinfowler.com/bliki/CQRS.html)

### .NET
- [ASP.NET Core Best Practices](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/best-practices)
- [Dapper Documentation](https://github.com/DapperLib/Dapper)
- [MediatR Wiki](https://github.com/jbogard/MediatR/wiki)

### Domain-Driven Design
- [Domain-Driven Design by Eric Evans](https://www.domainlanguage.com/ddd/)
- [Implementing DDD by Vaughn Vernon](https://vaughnvernon.com/)

## 🤝 When to Ask for Help

**Ask for help when:**
- Architecture decision impacts multiple layers
- Security concern or vulnerability
- Performance bottleneck
- Breaking API change
- Complex domain logic

**How to ask:**
- Provide context (what you're trying to achieve)
- Show what you've tried
- Include relevant code snippets
- Link to related issues/PRs

---

## 🎯 Current Sprint Focus

**Sprint 1: Foundation & Authentication** ✅ IN PROGRESS
- [x] Project structure setup
- [x] Clean Architecture scaffolding
- [ ] User entity and value objects
- [ ] Registration command/handler
- [ ] Login command/handler
- [ ] JWT generation/validation
- [ ] Refresh token flow
- [ ] Basic API endpoints
- [ ] Integration tests

**Next Sprint: Guilds & Channels**
- [ ] Guild aggregate
- [ ] Channel entity
- [ ] Permissions system
- [ ] Member roles

---

**Last Updated**: 2026-02-15
**Maintainer**: Harmonie Core Team
**Questions?**: Open an issue or ask in Discord (ironic, we know 😄)
