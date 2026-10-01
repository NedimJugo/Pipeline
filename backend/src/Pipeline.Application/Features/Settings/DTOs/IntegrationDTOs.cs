namespace Pipeline.Application.Features.Settings.DTOs;

public record IntegrationSettingsDto(
    bool UseCustomSmtp,
    string? SmtpHost,
    int SmtpPort,
    string? SmtpUser,
    bool HasSmtpPassword,
    string? SmtpFrom,
    bool IsEnvFallbackSmtp,
    bool UseCustomGoogle,
    string? GoogleClientId,
    bool HasGoogleClientSecret,
    string StorageProvider
);

public record UpdateIntegrationSettingsRequest(
    bool? UseCustomSmtp,
    string? SmtpHost,
    int? SmtpPort,
    string? SmtpUser,
    string? SmtpPassword,
    string? SmtpFrom,
    bool? UseCustomGoogle,
    string? GoogleClientId,
    string? GoogleClientSecret,
    string? StorageProvider
);

public record TestEmailRequest(string TargetEmail);

public record TestEmailResultDto(bool Success, string Message);
