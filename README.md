# Harmonie 🎵

> Open-source, self-hosted communication platform. From discord to harmonie.

[![CI](https://github.com/harmonie-chat/harmonie/actions/workflows/ci.yml/badge.svg)](https://github.com/harmonie-chat/harmonie/actions)
[![License: AGPL-3.0](https://img.shields.io/badge/license-AGPL--3.0-blue.svg)](LICENSE)
[![.NET 10](https://img.shields.io/badge/.NET-10-purple)](https://dotnet.microsoft.com)

## 🌟 Features

- ✅ **Self-hosted**: Full control over your data
- ✅ **Multi-server**: Connect to multiple Harmonie instances from one client
- ✅ **Real-time**: Powered by SignalR
- 🚧 **Voice & Video**: Coming soon with LiveKit integration
- ✅ **Open-source**: AGPL-3.0 licensed

## 🏗️ Architecture

Harmonie follows **Clean Architecture** principles with **Vertical Slice** pattern:

```
┌─────────────────────────────────────┐
│   API Layer (Minimal APIs)         │  ← HTTP endpoints
├─────────────────────────────────────┤
│   Application (Vertical Slices)    │  ← Features organized by domain
├─────────────────────────────────────┤
│   Domain Layer (Entities)          │  ← Pure business logic
├─────────────────────────────────────┤
│   Infrastructure (Dapper, JWT)     │  ← Database, Auth, External
└─────────────────────────────────────┘
```

**Key Principles:**
- Domain-driven design (DDD)
- Vertical Slice Architecture (feature-based organization)
- Repository pattern with Dapper
- JWT authentication with refresh tokens
- No MediatR - direct, explicit code flow

📖 **[Learn more about our architecture →](VERTICAL_SLICE_ARCHITECTURE.md)**

## 🚀 Quick Start

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Docker](https://www.docker.com/get-started) (for PostgreSQL)
- [PostgreSQL 18.2](https://www.postgresql.org/) (or use Docker Compose)

### Local Development

1. **Clone the repository**
```bash
git clone https://github.com/harmonie-chat/harmonie.git
cd harmonie
```

2. **Start PostgreSQL with Docker Compose**
```bash
docker-compose up -d postgres
```

3. **Run migrations**
```bash
cd tools/Harmonie.Migrations
dotnet run
```

4. **Run the API**
```bash
cd src/Harmonie.API
dotnet run
```

5. **API is available at**
```
https://localhost:7001
http://localhost:5001
```

### Running Tests

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test /p:CollectCoverage=true /p:CoverageReportsFormat=opencover
```

## 📁 Project Structure

```
Harmonie/
├── src/
│   ├── Harmonie.Domain/              # Core business logic, entities
│   ├── Harmonie.Application/         # Use cases, CQRS handlers
│   ├── Harmonie.Infrastructure/      # Database, external services
│   └── Harmonie.API/                 # REST API, SignalR hubs
├── tests/
│   ├── Harmonie.Domain.Tests/
│   ├── Harmonie.Application.Tests/
│   └── Harmonie.API.IntegrationTests/
├── tools/
│   └── Harmonie.Migrations/          # Database migrations
├── docs/                             # Documentation
├── docker/                           # Docker configurations
└── .github/                          # CI/CD workflows
```

## 🔐 Authentication Flow

Harmonie uses JWT with refresh tokens:

1. **Register**: `POST /api/auth/register`
2. **Login**: `POST /api/auth/login` → Returns `accessToken` + `refreshToken`
3. **Access protected routes**: Send `Authorization: Bearer {accessToken}`
4. **Refresh**: `POST /api/auth/refresh` → New `accessToken` when expired

## 🗄️ Database

- **RDBMS**: PostgreSQL 18.2
- **ORM**: Dapper (micro-ORM for performance)
- **Migrations**: DbUp for version control

### Connection String

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=harmonie;Username=harmonie_user;Password=your_secure_password"
  }
}
```

## 🐳 Docker Deployment

### Development
```bash
docker-compose up
```

### Production
```bash
docker-compose -f docker-compose.prod.yml up -d
```

## 🛠️ Configuration

Key configuration in `appsettings.json`:

```json
{
  "Jwt": {
    "Secret": "your-256-bit-secret-key-here-change-in-production",
    "Issuer": "harmonie",
    "Audience": "harmonie-client",
    "AccessTokenExpirationMinutes": 15,
    "RefreshTokenExpirationDays": 30
  },
  "Database": {
    "ConnectionString": "..."
  }
}
```

## 📚 Documentation

- [Architecture Decision Records (ADR)](docs/adr/)
- [API Documentation](docs/api/)
- [Deployment Guide](docs/deployment.md)
- [Contributing Guide](CONTRIBUTING.md)

## 🤝 Contributing

We welcome contributions! Please see [CONTRIBUTING.md](CONTRIBUTING.md) for details.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add some amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## 📄 License

This project is licensed under the AGPL-3.0 License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- Inspired by Discord, Revolt, and Matrix
- Built with ❤️ by the open-source community

## 🔗 Links

- **Website**: [harmonie.chat](https://harmonie.chat) (coming soon)
- **Documentation**: [docs.harmonie.chat](https://docs.harmonie.chat) (coming soon)
- **Discord Community**: [Join us](https://discord.gg/harmonie) (ironic, we know 😄)

---

**Made with 🎵 for free, open communication**
