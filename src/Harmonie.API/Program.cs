using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Harmonie.API.Endpoints;
using Harmonie.API.Middleware;
using Harmonie.Application;
using Harmonie.Application.Common;
using Harmonie.API.Configuration;
using Harmonie.Application.Features.Uploads.UploadFile;
using Harmonie.API.RealTime.Channels;
using Harmonie.API.RealTime.Common;
using Harmonie.API.RealTime.Conversations;
using Harmonie.API.RealTime.Guilds;
using Harmonie.API.RealTime.Messages;
using Harmonie.API.RealTime.Users;
using Harmonie.API.RealTime.Voice;
using Harmonie.Application.Interfaces.Channels;
using Harmonie.Application.Interfaces.Common;
using Harmonie.Application.Interfaces.Conversations;
using Harmonie.Application.Interfaces.Guilds;
using Harmonie.Application.Interfaces.Messages;
using Harmonie.Application.Interfaces.Users;
using Harmonie.Application.Interfaces.Voice;
using Harmonie.Infrastructure;
using Harmonie.Infrastructure.HealthChecks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Saunter;
using Saunter.AsyncApiSchema.v2;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// Add layers
builder.Services.AddApplication();
builder.Services.Configure<CorsSettings>(builder.Configuration.GetSection("Cors"));
builder.Services.Configure<UploadOptions>(builder.Configuration.GetSection("Uploads"));
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false));
});
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddSignalR();
builder.Services.AddAsyncApiSchemaGeneration(options =>
{
    options.AssemblyMarkerTypes = new[] { typeof(RealtimeHubDocumentation) };
    options.Middleware.UiTitle = "Harmonie Realtime API";
    options.AsyncApi = new AsyncApiDocument
    {
        Info = new Info("Harmonie Realtime API", "1.0.0")
        {
            Description = "Real-time events for the Harmonie communication platform, served over SignalR (WebSocket).",
        },
        Servers =
        {
            ["signalr"] = new Server("/hubs/realtime", "ws")
            {
                Description = "SignalR hub — requires Bearer JWT via the access_token query parameter.",
            },
        },
    };
});
builder.Services.AddScoped<ITextChannelNotifier, SignalRTextChannelNotifier>();
builder.Services.AddScoped<IGuildNotifier, SignalRGuildNotifier>();
builder.Services.AddScoped<IVoicePresenceNotifier, SignalRVoicePresenceNotifier>();
builder.Services.AddScoped<IConversationMessageNotifier, SignalRConversationMessageNotifier>();
builder.Services.AddScoped<IUserPresenceNotifier, SignalRUserPresenceNotifier>();
builder.Services.AddScoped<IReactionNotifier, SignalRReactionNotifier>();
builder.Services.AddSingleton<IConnectionTracker, ConnectionTracker>();
builder.Services.AddScoped<IRealtimeGroupManager, SignalRRealtimeGroupManager>();
builder.Services.AddHealthChecks()
    .AddCheck<PostgresHealthCheck>("postgres")
    .AddCheck<LiveKitHealthCheck>("livekit");
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("message-post", httpContext =>
    {
        var partitionKey = ResolveMessagePostPartitionKey(httpContext);
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 40,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                AutoReplenishment = true
            });
    });
});

// Configure Swagger with JWT support
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Info = new()
        {
            Title = "Harmonie API",
            Version = "v1",
            Description = "Open-source, self-hosted communication platform API"
        };

        return Task.CompletedTask;
    });
});

// Configure JWT Authentication
var jwtSecret = builder.Configuration["Jwt:Secret"];
if (string.IsNullOrWhiteSpace(jwtSecret))
    throw new InvalidOperationException("Configuration value 'Jwt:Secret' is required.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrWhiteSpace(accessToken)
                    && path.StartsWithSegments("/hubs/realtime"))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            },
            OnChallenge = context =>
            {
                context.HandleResponse();

                return EndpointExtensions.WriteErrorAsync(
                    context.Response,
                    new ApplicationError(
                        ApplicationErrorCodes.Auth.InvalidCredentials,
                        "Authentication is required to access this resource."));
            }
        };

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// CORS
var corsSettings = builder.Configuration.GetSection("Cors").Get<CorsSettings>() ?? new CorsSettings();
var allowedOrigins = corsSettings.AllowedOrigins
    .Where(origin => !string.IsNullOrWhiteSpace(origin))
    .Select(origin => origin.Trim())
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToArray();

builder.Services.AddCors(options =>
{
    options.AddPolicy("ApiCors", policy =>
    {
        if (builder.Environment.IsDevelopment()
            && allowedOrigins.Contains("*", StringComparer.Ordinal))
        {
            policy.SetIsOriginAllowed(_ => true)
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials();
            return;
        }

        var configuredOrigins = allowedOrigins
            .Where(origin => !string.Equals(origin, "*", StringComparison.Ordinal))
            .ToArray();

        if (configuredOrigins.Length > 0)
        {
            policy.WithOrigins(configuredOrigins)
                .AllowCredentials()
                .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS")
                .WithHeaders("Authorization", "Content-Type");
        }
    });
});

var app = builder.Build();

// Middleware pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
    app.MapAsyncApiDocuments();
    app.MapAsyncApiUi();
}

app.UseMiddleware<GlobalExceptionHandler>();
app.UseSerilogRequestLogging();
app.UseHttpsRedirection();
app.UseCors("ApiCors");
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<AuthenticatedUserContextMiddleware>();
app.UseRateLimiter();

// Map endpoints
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = WriteHealthCheckResponseAsync
})
.WithName("HealthCheck")
.WithTags("System");

app.MapAuthEndpoints();
app.MapGuildEndpoints();
app.MapChannelEndpoints();
app.MapConversationEndpoints();
app.MapUserEndpoints();
app.MapUploadEndpoints();
app.MapVoiceEndpoints();
app.MapHub<RealtimeHub>("/hubs/realtime");

app.Run();

static string ResolveMessagePostPartitionKey(HttpContext httpContext)
{
    if (httpContext.TryGetAuthenticatedUserId(out var userId) && userId is not null)
        return $"user:{userId}";

    var remoteIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    return $"ip:{remoteIp}";
}

static Task WriteHealthCheckResponseAsync(HttpContext httpContext, HealthReport report)
{
    httpContext.Response.ContentType = "application/json";

    var payload = new
    {
        status = report.Status.ToString(),
        timestamp = DateTime.UtcNow,
        checks = report.Entries.ToDictionary(
            entry => entry.Key,
            entry => new
            {
                status = entry.Value.Status.ToString(),
                description = entry.Value.Description
            })
    };

    return httpContext.Response.WriteAsync(JsonSerializer.Serialize(payload));
}

// Make Program class accessible to integration tests
public partial class Program { }
