# Contributing to Harmonie 🎵

Thank you for considering contributing to Harmonie! We welcome contributions from everyone.

## 📋 Table of Contents

- [Code of Conduct](#code-of-conduct)
- [How Can I Contribute?](#how-can-i-contribute)
- [Development Setup](#development-setup)
- [Pull Request Process](#pull-request-process)
- [Coding Standards](#coding-standards)
- [Commit Message Guidelines](#commit-message-guidelines)

## 📜 Code of Conduct

This project adheres to a Code of Conduct. By participating, you are expected to uphold this code. Please report unacceptable behavior to the maintainers.

## 🤝 How Can I Contribute?

### Reporting Bugs

- Use GitHub Issues to report bugs
- Include steps to reproduce the issue
- Provide environment details (.NET version, OS, etc.)
- Include relevant logs or error messages

### Suggesting Enhancements

- Open an issue with the `enhancement` label
- Describe the feature and its benefits
- Provide examples or mockups if applicable

### Code Contributions

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Make your changes
4. Write or update tests
5. Ensure all tests pass
6. Commit your changes (see commit guidelines below)
7. Push to your fork
8. Open a Pull Request

## 🛠️ Development Setup

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Docker](https://www.docker.com/get-started)
- [Git](https://git-scm.com/)

### Local Setup

```bash
# Clone the repository
git clone https://github.com/harmonie-chat/harmonie.git
cd harmonie

# Start PostgreSQL
docker-compose up -d postgres

# Run migrations
cd tools/Harmonie.Migrations
dotnet run

# Run the API
cd ../../src/Harmonie.API
dotnet run

# Run tests
dotnet test
```

## 🔄 Pull Request Process

1. **Update Documentation**: If you change APIs, update the relevant documentation
2. **Add Tests**: All new features must include unit tests
3. **Follow Coding Standards**: Ensure your code follows our standards (see below)
4. **Update agent.md**: If you make architectural decisions, document them
5. **Describe Your Changes**: Provide a clear PR description
6. **Link Related Issues**: Reference any related issues in the PR

### PR Checklist

- [ ] Tests pass locally (`dotnet test`)
- [ ] Code follows style guidelines
- [ ] Self-review completed
- [ ] Comments added for complex logic
- [ ] Documentation updated
- [ ] No new warnings introduced

## 📐 Coding Standards

### General Principles

- **English only**: Code, comments, docs, commit messages
- **Explicit > Implicit**: Favor clarity over cleverness
- **Clean Architecture**: Respect layer boundaries
- **SOLID Principles**: Follow object-oriented design principles
- **Async all the way**: No sync-over-async

### Naming Conventions

```csharp
// Classes, Records, Interfaces
public sealed class UserRepository { }
public record CreateUserCommand { }
public interface IUserRepository { }

// Methods
public async Task<User?> GetByIdAsync(UserId id) { }

// Private fields
private readonly IUserRepository _userRepository;

// Constants
public const int MaxUsernameLength = 32;
```

### File Organization

- One class per file
- Group related files in feature folders
- Use meaningful file and folder names

### Testing

- Use xUnit for tests
- Follow Arrange-Act-Assert pattern
- Use FluentAssertions for readability
- Aim for >80% code coverage

```csharp
[Fact]
public void Create_WithValidData_ShouldSucceed()
{
    // Arrange
    var email = Email.Create("test@harmonie.chat").Value!;
    
    // Act
    var result = User.Create(email, username, hash);
    
    // Assert
    result.IsSuccess.Should().BeTrue();
}
```

## 📝 Commit Message Guidelines

We follow [Conventional Commits](https://www.conventionalcommits.org/):

```
<type>: <description>

[optional body]

[optional footer]
```

### Types

- `feat`: New feature
- `fix`: Bug fix
- `docs`: Documentation changes
- `style`: Code style changes (formatting, etc.)
- `refactor`: Code refactoring
- `test`: Adding or updating tests
- `chore`: Maintenance tasks

### Examples

```bash
feat: add user registration endpoint

Implements user registration with email verification.
Includes validation, password hashing, and JWT generation.

Closes #42
```

```bash
fix: resolve JWT token expiration issue

Previously, tokens were expiring 5 minutes early due to
clock skew. Now using ClockSkew = TimeSpan.Zero.
```

## 🎯 Areas Needing Help

Check our [GitHub Issues](https://github.com/harmonie-chat/harmonie/issues) for areas where we need help:

- `good first issue`: Great for newcomers
- `help wanted`: We'd appreciate community input
- `enhancement`: New features to implement
- `bug`: Known bugs that need fixing

## 💬 Questions?

- Open a [GitHub Discussion](https://github.com/harmonie-chat/harmonie/discussions)
- Join our Discord (link in README)
- Email the maintainers

## 🙏 Thank You!

Your contributions make Harmonie better for everyone. We appreciate your time and effort!

---

**Happy coding! 🎵**
