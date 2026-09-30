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
using Pipeline.Application.Features.Contacts.Services;
using Pipeline.Application.Features.Interactions.Services;
using Pipeline.Application.Features.Interviews.Services;
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

// Storage Configuration
var storageProvider = builder.Configuration["Storage:Provider"] ?? builder.Configuration["Storage__Provider"] ?? "Local";
var storageEndpoint = builder.Configuration["Storage:Endpoint"] ?? builder.Configuration["Storage__Endpoint"];
var storageBucket = builder.Configuration["Storage:Bucket"] ?? builder.Configuration["Storage__Bucket"] ?? "pipeline";
var storageAccessKey = builder.Configuration["Storage:AccessKey"] ?? builder.Configuration["Storage__AccessKey"] ?? "minio";
var storageSecretKey = builder.Configuration["Storage:SecretKey"] ?? builder.Configuration["Storage__SecretKey"] ?? "minioadmin";

if (string.Equals(storageProvider, "S3", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(storageEndpoint))
{
    var s3Config = new Amazon.S3.AmazonS3Config
    {
        ServiceURL = storageEndpoint,
        ForcePathStyle = true,
        UseHttp = storageEndpoint.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
    };
    var credentials = new Amazon.Runtime.BasicAWSCredentials(storageAccessKey, storageSecretKey);
    builder.Services.AddSingleton<Amazon.S3.IAmazonS3>(new Amazon.S3.AmazonS3Client(credentials, s3Config));
    builder.Services.AddSingleton<IFileStorage>(sp => new Pipeline.Infrastructure.Services.Storage.S3FileStorage(sp.GetRequiredService<Amazon.S3.IAmazonS3>(), storageBucket));
}
else
{
    builder.Services.AddSingleton<IFileStorage>(new Pipeline.Infrastructure.Services.Storage.LocalFileStorage());
}

// Application Services
builder.Services.AddSingleton<JwtTokenGenerator>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICompanyService, CompanyService>();
builder.Services.AddScoped<IApplicationService, ApplicationService>();
builder.Services.AddScoped<IContactService, ContactService>();
builder.Services.AddScoped<IInteractionService, InteractionService>();
builder.Services.AddScoped<IInterviewService, InterviewService>();
builder.Services.AddScoped<Pipeline.Application.Features.Documents.Services.IDocumentService, DocumentService>();
builder.Services.AddScoped<Pipeline.Application.Features.Tasks.Services.ITaskService, TaskService>();
builder.Services.AddScoped<Pipeline.Application.Features.Templates.Services.IEmailTemplateService, EmailTemplateService>();
builder.Services.AddScoped<Pipeline.Application.Features.Automation.Services.IAutomationRuleEngine, AutomationRuleEngine>();
builder.Services.AddScoped<Pipeline.Application.Features.Dashboard.Services.IDashboardService, DashboardService>();
builder.Services.AddScoped<Pipeline.Application.Features.References.Services.IReferenceService, ReferenceService>();
builder.Services.AddScoped<Pipeline.Application.Features.Offers.Services.IOfferService, OfferService>();
builder.Services.AddScoped<Pipeline.Application.Features.Analytics.Services.IAnalyticsService, AnalyticsService>();
builder.Services.AddScoped<Pipeline.Application.Features.Calendar.Services.ICalendarService, CalendarService>();
builder.Services.AddScoped<Pipeline.Application.Features.Users.Services.IUserService, UserService>();
builder.Services.AddScoped<Pipeline.Application.Features.Discovery.Services.IJobSourceConnector, SampleJobConnector>();
builder.Services.AddScoped<Pipeline.Application.Features.Discovery.Services.IJobDiscoveryService, JobDiscoveryService>();
builder.Services.AddHostedService<AutomationBackgroundService>();

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
