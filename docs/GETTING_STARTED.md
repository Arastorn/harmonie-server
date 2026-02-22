# 🚀 Quick Start Guide

Get Harmonie running locally in 5 minutes!

## Prerequisites

✅ [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)  
✅ [Docker](https://www.docker.com/get-started) (for PostgreSQL)  
✅ Your favorite IDE (VS Code, Rider, Visual Studio)

## Step 1: Clone & Setup

```bash
git clone https://github.com/harmonie-chat/harmonie.git
cd harmonie
```

## Step 2: Start PostgreSQL

```bash
docker-compose up -d postgres
```

Wait ~10 seconds for PostgreSQL to be ready. Verify with:
```bash
docker-compose ps
```

You should see `harmonie-postgres` with status `Up (healthy)`.

## Step 3: Run Database Migrations

```bash
cd tools/Harmonie.Migrations
dotnet run
```

Expected output:
```
✅ Success!
```

## Step 4: Run the API

```bash
cd ../../src/Harmonie.API
dotnet run
```

The API will start on:
- **HTTPS**: `https://localhost:7001`
- **HTTP**: `http://localhost:5001`

## Step 5: Test the API

### Open Swagger UI

Navigate to: `https://localhost:7001/swagger`

### Register a User

**POST** `/api/auth/register`

```json
{
  "email": "test@harmonie.chat",
  "username": "testuser",
  "password": "Test123!@#"
}
```

**Response** (201 Created):
```json
{
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "email": "test@harmonie.chat",
  "username": "testuser",
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "base64encodedtoken",
  "expiresAt": "2026-02-15T15:30:00Z"
}
```

### Login

**POST** `/api/auth/login`

```json
{
  "emailOrUsername": "testuser",
  "password": "Test123!@#"
}
```

### Use Protected Endpoints (Future)

Copy the `accessToken` from login response, then:

1. Click **Authorize** button in Swagger
2. Enter: `Bearer {your_access_token}`
3. Click **Authorize**

Now you can access protected endpoints! 🎉

## 🧪 Run Tests

```bash
# From project root
dotnet test

# With coverage
dotnet test /p:CollectCoverage=true
```

## 🐛 Troubleshooting

### PostgreSQL connection fails

```bash
# Check if PostgreSQL is running
docker-compose ps

# View logs
docker-compose logs postgres

# Restart if needed
docker-compose restart postgres
```

### Port already in use

If ports 5001/7001 are taken, edit `src/Harmonie.API/Properties/launchSettings.json`:

```json
{
  "applicationUrl": "https://localhost:YOUR_PORT;http://localhost:YOUR_PORT"
}
```

### Migration fails

```bash
# Reset database
docker-compose down -v
docker-compose up -d postgres

# Wait 10 seconds, then retry migration
cd tools/Harmonie.Migrations
dotnet run
```

## 📚 Next Steps

- Read the [Architecture Documentation](agent.md)
- Check [Contributing Guidelines](CONTRIBUTING.md)
- Join our [Discord Community](#)
- Report issues on [GitHub](https://github.com/harmonie-chat/harmonie/issues)

## 🔑 Default Configuration

```json
{
  "ConnectionString": "Host=localhost;Port=5432;Database=harmonie;Username=harmonie_user;Password=harmonie_password",
  "JWT": {
    "AccessTokenExpiration": "15 minutes",
    "RefreshTokenExpiration": "30 days"
  }
}
```

⚠️ **Change these values in production!**

## 💡 Pro Tips

1. **Hot Reload**: Use `dotnet watch run` for automatic restarts on code changes
2. **Database GUI**: Connect with pgAdmin or DBeaver using credentials above
3. **Logs**: Check console output for detailed request/response logs
4. **API Testing**: Use Bruno as alternatives to Swagger

---

**Happy coding! 🎵**

Need help? Open an issue or ask in Discussions!
