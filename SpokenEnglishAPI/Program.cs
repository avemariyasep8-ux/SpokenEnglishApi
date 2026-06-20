using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using SpokenEnglishAPI.Application.Implementation;
using SpokenEnglishAPI.Application.Interfaces;
using SpokenEnglishAPI.Application.Services;
using SpokenEnglishAPI.Infrastructure.Data;
using SpokenEnglishAPI.Infrastructure.Repositories;
using SpokenEnglishAPI.Infrastructure.Security;

namespace SpokenEnglishAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

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
                    policy.WithOrigins(allowedOrigins.Concat(new[] {
                              "http://localhost:5173","http://localhost:5174",
                              "http://localhost:5175","http://localhost:5176"
                          }).ToArray())
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials());
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

            // ---------------------------------
            // JWT CONFIG
            // ---------------------------------
            var jwtKey = builder.Configuration["Jwt:Key"];
            var jwtIssuer = builder.Configuration["Jwt:Issuer"];
            var jwtAudience = builder.Configuration["Jwt:Audience"];

            if (string.IsNullOrWhiteSpace(jwtKey))
                throw new InvalidOperationException("Jwt:Key is missing in appsettings.json");
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
            // Build app
            // ---------------------------------
            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            // CORS must come before Auth
            app.UseCors("ReactPolicy");

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();
            app.Run();
        }
    }
}
