using System;
using System.Threading;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MimeKit;
using Pipeline.Application.Common.Interfaces;
using Pipeline.Application.Features.Settings.DTOs;
using Pipeline.Application.Features.Settings.Services;
using Pipeline.Domain.Entities;
using Pipeline.Infrastructure.Persistence;

namespace Pipeline.Infrastructure.Services;

public class IntegrationService : IIntegrationService
{
    private readonly PipelineDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IConfiguration _configuration;

    public IntegrationService(
        PipelineDbContext dbContext,
        ICurrentUserService currentUserService,
        IConfiguration configuration)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _configuration = configuration;
    }

    public async Task<IntegrationSettingsDto> GetSettingsAsync(CancellationToken ct = default)
    {
        var userId = _currentUserService.UserId ?? Guid.Empty;
        var customSetting = await _dbContext.UserIntegrationSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == userId, ct);

        // System environment fallbacks
        var envSmtpHost = _configuration["Smtp:Host"] ?? _configuration["Smtp__Host"] ?? "localhost";
        var envSmtpPortStr = _configuration["Smtp:Port"] ?? _configuration["Smtp__Port"] ?? "1025";
        int.TryParse(envSmtpPortStr, out var envSmtpPort);
        if (envSmtpPort == 0) envSmtpPort = 1025;
        var envSmtpFrom = _configuration["Smtp:From"] ?? _configuration["Smtp__From"] ?? "no-reply@pipeline.local";
        var envGoogleClientId = _configuration["Google:ClientId"] ?? _configuration["Google__ClientId"] ?? string.Empty;
        var envStorageProvider = _configuration["Storage:Provider"] ?? _configuration["Storage__Provider"] ?? "Local";

        if (customSetting == null)
        {
            return new IntegrationSettingsDto(
                UseCustomSmtp: false,
                SmtpHost: envSmtpHost,
                SmtpPort: envSmtpPort,
                SmtpUser: null,
                HasSmtpPassword: false,
                SmtpFrom: envSmtpFrom,
                IsEnvFallbackSmtp: true,
                UseCustomGoogle: false,
                GoogleClientId: envGoogleClientId,
                HasGoogleClientSecret: false,
                StorageProvider: envStorageProvider
            );
        }

        var isCustomSmtp = customSetting.UseCustomSmtp;
        var effectiveHost = isCustomSmtp ? (customSetting.SmtpHost ?? envSmtpHost) : envSmtpHost;
        var effectivePort = isCustomSmtp ? (customSetting.SmtpPort ?? envSmtpPort) : envSmtpPort;
        var effectiveFrom = isCustomSmtp ? (customSetting.SmtpFrom ?? envSmtpFrom) : envSmtpFrom;
        var effectiveUser = isCustomSmtp ? customSetting.SmtpUser : null;
        var hasSmtpPass = !string.IsNullOrEmpty(customSetting.SmtpPassword);

        var isCustomGoogle = customSetting.UseCustomGoogle;
        var effectiveGoogleId = isCustomGoogle ? (customSetting.GoogleClientId ?? envGoogleClientId) : envGoogleClientId;
        var hasGoogleSecret = !string.IsNullOrEmpty(customSetting.GoogleClientSecret);

        var effectiveStorage = !string.IsNullOrEmpty(customSetting.StorageProvider)
            ? customSetting.StorageProvider
            : envStorageProvider;

        return new IntegrationSettingsDto(
            UseCustomSmtp: isCustomSmtp,
            SmtpHost: effectiveHost,
            SmtpPort: effectivePort,
            SmtpUser: effectiveUser,
            HasSmtpPassword: hasSmtpPass,
            SmtpFrom: effectiveFrom,
            IsEnvFallbackSmtp: !isCustomSmtp,
            UseCustomGoogle: isCustomGoogle,
            GoogleClientId: effectiveGoogleId,
            HasGoogleClientSecret: hasGoogleSecret,
            StorageProvider: effectiveStorage
        );
    }

    public async Task<IntegrationSettingsDto> UpdateSettingsAsync(UpdateIntegrationSettingsRequest request, CancellationToken ct = default)
    {
        var userId = _currentUserService.UserId ?? Guid.Empty;
        var setting = await _dbContext.UserIntegrationSettings
            .FirstOrDefaultAsync(s => s.UserId == userId, ct);

        if (setting == null)
        {
            setting = new UserIntegrationSetting
            {
                UserId = userId
            };
            _dbContext.UserIntegrationSettings.Add(setting);
        }

        if (request.UseCustomSmtp.HasValue)
            setting.UseCustomSmtp = request.UseCustomSmtp.Value;

        if (request.SmtpHost != null)
            setting.SmtpHost = request.SmtpHost.Trim();

        if (request.SmtpPort.HasValue)
            setting.SmtpPort = request.SmtpPort.Value;

        if (request.SmtpUser != null)
            setting.SmtpUser = request.SmtpUser.Trim();

        if (!string.IsNullOrEmpty(request.SmtpPassword))
            setting.SmtpPassword = request.SmtpPassword;

        if (request.SmtpFrom != null)
            setting.SmtpFrom = request.SmtpFrom.Trim();

        if (request.UseCustomGoogle.HasValue)
            setting.UseCustomGoogle = request.UseCustomGoogle.Value;

        if (request.GoogleClientId != null)
            setting.GoogleClientId = request.GoogleClientId.Trim();

        if (!string.IsNullOrEmpty(request.GoogleClientSecret))
            setting.GoogleClientSecret = request.GoogleClientSecret;

        if (request.StorageProvider != null)
            setting.StorageProvider = request.StorageProvider.Trim();

        await _dbContext.SaveChangesAsync(ct);

        return await GetSettingsAsync(ct);
    }

    public async Task<TestEmailResultDto> SendTestEmailAsync(TestEmailRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.TargetEmail))
        {
            return new TestEmailResultDto(false, "Recipient email address is required.");
        }

        var settings = await GetSettingsAsync(ct);
        var userId = _currentUserService.UserId ?? Guid.Empty;
        var customSetting = await _dbContext.UserIntegrationSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == userId, ct);

        var host = settings.SmtpHost ?? "localhost";
        var port = settings.SmtpPort;
        var from = settings.SmtpFrom ?? "no-reply@pipeline.local";
        var user = settings.SmtpUser;
        var password = customSetting?.SmtpPassword;

        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Pipeline Command Center", from));
            message.To.Add(new MailboxAddress("", request.TargetEmail));
            message.Subject = "Pipeline — SMTP Test Connection";

            var bodyBuilder = new BodyBuilder
            {
                TextBody = $"Hello!\n\nThis is a test notification confirming that your SMTP integration in Pipeline Command Center is working properly.\n\nHost: {host}:{port}\nSender: {from}\nTimestamp: {DateTime.UtcNow:u}\n\nHappy job hunting!\n— Pipeline Team"
            };
            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            client.ServerCertificateValidationCallback = (s, c, h, e) => true;

            var secureOptions = port == 465 
                ? SecureSocketOptions.SslOnConnect 
                : port == 587 
                    ? SecureSocketOptions.StartTls 
                    : SecureSocketOptions.Auto;

            await client.ConnectAsync(host, port, secureOptions, ct);

            if (!string.IsNullOrEmpty(user) && !string.IsNullOrEmpty(password))
            {
                await client.AuthenticateAsync(user, password, ct);
            }

            await client.SendAsync(message, ct);
            await client.DisconnectAsync(true, ct);

            return new TestEmailResultDto(true, $"Test email successfully sent to {request.TargetEmail} via {host}:{port}!");
        }
        catch (Exception ex)
        {
            return new TestEmailResultDto(false, $"SMTP connection failed: {ex.Message}");
        }
    }
}
