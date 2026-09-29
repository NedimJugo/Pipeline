using System;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Pipeline.Api.Middleware;
using Pipeline.Api.Services;
using Pipeline.Application.Common.Interfaces;
using Pipeline.Application.Features.Applications.Services;
using Pipeline.Application.Features.Auth.Services;
using Pipeline.Application.Features.Companies.Services;
using Pipeline.Domain.Entities;
using Pipeline.Infrastructure.Persistence;
using Pipeline.Infrastructure.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// Add HttpContextAccessor & CurrentUserService
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// Database configuration
var defaultConnection = builder.Configuration.GetConnectionString("Default") 
    ?? builder.Configuration["ConnectionStrings__Default"]
    ?? "Host=localhost;Port=5432;Database=pipeline;Username=pipeline;Password=change-me";

builder.Services.AddDbContext<PipelineDbContext>((sp, options) =>
{
    if (defaultConnection.StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase) || defaultConnection.EndsWith(".db", StringComparison.OrdinalIgnoreCase))
    {
        options.UseSqlite(defaultConnection);
    }
    else
    {
        options.UseNpgsql(defaultConnection);
    }
});

// Identity configuration
builder.Services.AddIdentity<User, IdentityRole<Guid>>(options =>
{
    options.Password.RequiredLength = 10;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireDigit = false;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<PipelineDbContext>()
.AddDefaultTokenProviders();

// JWT Authentication configuration
var jwtSecret = builder.Configuration["Jwt:Secret"] 
    ?? builder.Configuration["Jwt__Secret"] 
    ?? "very-secure-jwt-secret-key-that-is-at-least-32-chars-long!";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? builder.Configuration["Jwt__Issuer"] ?? "pipeline";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? builder.Configuration["Jwt__Audience"] ?? "pipeline-web";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// Application Services
builder.Services.AddSingleton<JwtTokenGenerator>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICompanyService, CompanyService>();
builder.Services.AddScoped<IApplicationService, ApplicationService>();

// CORS Configuration
var frontendOrigin = builder.Configuration["Frontend:Origin"] 
    ?? builder.Configuration["Frontend__Origin"] 
    ?? "http://localhost:5173";

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendCorsPolicy", policy =>
    {
        policy.WithOrigins(frontendOrigin, "http://localhost:5173", "http://127.0.0.1:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

// Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Pipeline API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Auto-migrate if requested and relational
var autoMigrate = builder.Configuration.GetValue<bool>("AUTO_MIGRATE", true);
if (autoMigrate)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<PipelineDbContext>();
    if (db.Database.IsRelational())
    {
        try
        {
            if (db.Database.IsSqlite())
            {
                db.Database.EnsureCreated();
            }
            else
            {
                db.Database.Migrate();
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to apply database migrations on startup.");
        }
    }
}

// Exception handling middleware
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Security headers
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    await next();
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("FrontendCorsPolicy");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program { }
