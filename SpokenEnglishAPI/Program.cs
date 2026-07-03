using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using NLog;
using NLog.Web;
using SpokenEnglishAPI.Application.Implementation;
using SpokenEnglishAPI.Application.Interfaces;
using SpokenEnglishAPI.Application.Services;
using SpokenEnglishAPI.Infrastructure.Data;
using SpokenEnglishAPI.Infrastructure.Repositories;
using SpokenEnglishAPI.Infrastructure.Security;
using SpokenEnglishAPI.Middleware;

namespace SpokenEnglishAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // Bootstrap NLog before the host builds so early startup errors are captured
            var logger = LogManager.Setup().LoadConfigurationFromFile("nlog.config").GetCurrentClassLogger();
            logger.Info("SpokenEnglish API starting");

            var builder = WebApplication.CreateBuilder(args);

            // Use NLog as the .NET ILogger provider
            builder.Logging.ClearProviders();
            builder.Host.UseNLog();

            // Hosts (Neon/Render/Railway/Supabase) provide DATABASE_URL as a postgres:// URI —
            // convert to Npgsql format. Managed Postgres requires SSL, so default to Require
            // (unless the URL explicitly says sslmode=disable). Port defaults to 5432 when absent.
            var dbUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
            if (!string.IsNullOrEmpty(dbUrl))
            {
                var uri = new Uri(dbUrl);
                var userInfo = uri.UserInfo.Split(':', 2);
                var port = uri.Port > 0 ? uri.Port : 5432;
                var sslMode = dbUrl.Contains("sslmode=disable", StringComparison.OrdinalIgnoreCase) ? "Disable" : "Require";
                var user = Uri.UnescapeDataString(userInfo[0]);
                var pass = Uri.UnescapeDataString(userInfo.Length > 1 ? userInfo[1] : "");
                var npgsql = $"Host={uri.Host};Port={port};Database={uri.AbsolutePath.TrimStart('/')};" +
                             $"Username={user};Password={pass};SSL Mode={sslMode};Trust Server Certificate=true";
                builder.Configuration["ConnectionStrings:SE_DB"] = npgsql;
            }

            // Railway uses PORT env var — only override URL binding in production
            var railwayPort = Environment.GetEnvironmentVariable("PORT");
            if (!string.IsNullOrEmpty(railwayPort))
                builder.WebHost.UseUrls($"http://0.0.0.0:{railwayPort}");

            // ---------------------------------
            // Add services to the container
            // ---------------------------------
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new() { Title = "Spoken English API", Version = "v1" });
                // JWT support in Swagger UI
                c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                    Description = "Enter: Bearer {token}",
                    Name = "Authorization",
                    Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });
                c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
                {
                    {
                        new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                        {
                            Reference = new Microsoft.OpenApi.Models.OpenApiReference
                            { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Bearer" }
                        },
                        Array.Empty<string>()
                    }
                });
            });

            // ---------------------------------
            // CORS — allow React dev server
            // ---------------------------------
            var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>()
                                 ?? new[] { "http://localhost:5173" };

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("ReactPolicy", policy =>
                {
                    // In production allow the Vercel UI domain (set via ALLOWED_ORIGIN env var)
                    var prodOrigin = Environment.GetEnvironmentVariable("ALLOWED_ORIGIN");
                    var origins = allowedOrigins.Concat(new[] {
                        "http://localhost:5173","http://localhost:5174",
                        "http://localhost:5175","http://localhost:5176"
                    });
                    if (!string.IsNullOrEmpty(prodOrigin)) origins = origins.Append(prodOrigin);

                    policy.WithOrigins(origins.ToArray())
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials();
                });
            });

            // ---------------------------------
            // Dependency Injection
            // ---------------------------------
            builder.Services.AddSingleton<DbContext>();

            // Existing
            builder.Services.AddScoped<UserRepository>();
            builder.Services.AddScoped<UsageRepository>();
            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<ISpeechService, SpeechService>();
            builder.Services.AddSingleton<JwtTokenGenerator>();

            // Learning features
            builder.Services.AddScoped<LessonRepository>();
            builder.Services.AddScoped<MeaningRepository>();
            builder.Services.AddScoped<ArrangeRepository>();
            builder.Services.AddScoped<ReadingRepository>();
            builder.Services.AddScoped<ProgressRepository>();

            builder.Services.AddScoped<ILessonService, LessonService>();
            builder.Services.AddScoped<IMeaningService, MeaningService>();
            builder.Services.AddScoped<IArrangeService, ArrangeService>();
            builder.Services.AddScoped<IReadingService, ReadingService>();
            builder.Services.AddScoped<IProgressService, ProgressService>();

            // New: word content, subscription, streak
            builder.Services.AddScoped<WordContentRepository>();
            builder.Services.AddScoped<SubscriptionRepository>();
            builder.Services.AddScoped<StreakRepository>();

            // Email alert service (exception notifications)
            builder.Services.AddSingleton<IEmailAlertService, EmailAlertService>();

            // Audit logging (login/register/admin actions)
            builder.Services.AddScoped<IAuditLogService, AuditLogService>();

            // ---------------------------------
            // JWT CONFIG
            // ---------------------------------
            // Prefer the JWT_KEY environment variable (set in Railway) over the value in
            // appsettings.json so the signing secret is never shipped in source control.
            var jwtKey = Environment.GetEnvironmentVariable("JWT_KEY");
            if (string.IsNullOrWhiteSpace(jwtKey))
                jwtKey = builder.Configuration["Jwt:Key"];
            // Make the resolved key available to JwtTokenGenerator (which reads Jwt:Key).
            builder.Configuration["Jwt:Key"] = jwtKey;

            var jwtIssuer = builder.Configuration["Jwt:Issuer"];
            var jwtAudience = builder.Configuration["Jwt:Audience"];

            if (string.IsNullOrWhiteSpace(jwtKey))
                throw new InvalidOperationException("Jwt:Key is missing in appsettings.json");
            // Warn loudly if a weak/short signing key is in use — HMAC-SHA256 needs >= 32 bytes.
            if (jwtKey.Length < 32 || jwtKey.Contains("SUPER_LONG_SECRET_KEY"))
                logger.Warn("WEAK JWT SIGNING KEY in use. Set a strong JWT_KEY env var (>= 32 random chars) in production.");
            if (string.IsNullOrWhiteSpace(jwtIssuer))
                throw new InvalidOperationException("Jwt:Issuer is missing in appsettings.json");
            if (string.IsNullOrWhiteSpace(jwtAudience))
                throw new InvalidOperationException("Jwt:Audience is missing in appsettings.json");

            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.RequireHttpsMetadata = false; // allow HTTP in development
                    options.SaveToken = true;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = jwtIssuer,
                        ValidAudience = jwtAudience,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                        ClockSkew = TimeSpan.Zero
                    };
                });

            builder.Services.AddAuthorization();

            // ---------------------------------
            // Rate limiting (100 req/min per IP, burst 20)
            // ---------------------------------
            builder.Services.AddRateLimiter(opts =>
            {
                opts.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
                    RateLimitPartition.GetSlidingWindowLimiter(
                        ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        _ => new SlidingWindowRateLimiterOptions
                        {
                            PermitLimit = 100,
                            Window = TimeSpan.FromMinutes(1),
                            SegmentsPerWindow = 6,
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = 5
                        }));
                opts.RejectionStatusCode = 429;
                opts.OnRejected = async (ctx2, _) =>
                {
                    ctx2.HttpContext.Response.Headers["Retry-After"] = "60";
                    await ctx2.HttpContext.Response.WriteAsJsonAsync(new
                    {
                        error = "RATE_LIMIT_EXCEEDED",
                        message = "Too many requests. Please wait 60 seconds."
                    });
                };
            });

            // ---------------------------------
            // Build app
            // ---------------------------------
            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            // Global exception handler — must be first so it wraps everything
            app.UseMiddleware<GlobalExceptionMiddleware>();

            // Security headers on every response
            app.UseMiddleware<SecurityHeadersMiddleware>();

            // Request/performance logging
            app.UseMiddleware<RequestLoggingMiddleware>();

            // Skip HTTPS redirect in production (Railway terminates TLS at load balancer)
            if (app.Environment.IsDevelopment()) app.UseHttpsRedirection();

            // Rate limiting before CORS/auth
            app.UseRateLimiter();

            // Stricter brute-force throttle for auth endpoints
            app.UseMiddleware<RateLimitMiddleware>();

            // CORS must come before Auth
            app.UseCors("ReactPolicy");

            app.UseAuthentication();
            app.UseAuthorization();

            // Health check for Railway
            app.MapGet("/health", () => Results.Ok(new { status = "healthy", time = DateTime.UtcNow }));

            app.MapControllers();
            app.Run();
        }
    }
}
