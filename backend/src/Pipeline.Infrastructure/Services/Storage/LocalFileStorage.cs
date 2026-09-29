using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Pipeline.Application.Common.Interfaces;

namespace Pipeline.Infrastructure.Services.Storage;

public class LocalFileStorage : IFileStorage
{
    private readonly string _storageDirectory;

    public LocalFileStorage(string? baseDirectory = null)
    {
        _storageDirectory = baseDirectory ?? Path.Combine(AppContext.BaseDirectory, "App_Data", "uploads");
        if (!Directory.Exists(_storageDirectory))
        {
            Directory.CreateDirectory(_storageDirectory);
        }
    }

    public async Task<string> UploadAsync(Stream stream, string fileName, string contentType, CancellationToken ct = default)
    {
        var validatedContentType = FileValidationService.ValidateAndNormalize(stream, fileName, contentType);

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        var fileKey = $"{Guid.NewGuid():N}{extension}";
        var destinationPath = Path.Combine(_storageDirectory, fileKey);

        using (var outputStream = File.Create(destinationPath))
        {
            if (stream.CanSeek)
            {
                stream.Position = 0;
            }
            await stream.CopyToAsync(outputStream, ct);
        }

        return fileKey;
    }

    public Task<FileDownloadResult> DownloadAsync(string fileKey, CancellationToken ct = default)
    {
        var sanitizedKey = Path.GetFileName(fileKey);
        var filePath = Path.Combine(_storageDirectory, sanitizedKey);

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Stored file with key '{fileKey}' was not found.");
        }

        var ext = Path.GetExtension(sanitizedKey).ToLowerInvariant();
        var contentType = ext switch
        {
            ".pdf" => "application/pdf",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            _ => "application/octet-stream"
        };

        var stream = File.OpenRead(filePath);
        return Task.FromResult(new FileDownloadResult(stream, contentType, sanitizedKey));
    }

    public Task<string> GetPresignedDownloadUrlAsync(string fileKey, string fileName, TimeSpan expiry, CancellationToken ct = default)
    {
        // For local storage, provide direct API stream endpoint
        var sanitizedKey = Path.GetFileName(fileKey);
        return Task.FromResult($"/api/document-versions/download-stream?key={Uri.EscapeDataString(sanitizedKey)}");
    }

    public Task DeleteAsync(string fileKey, CancellationToken ct = default)
    {
        var sanitizedKey = Path.GetFileName(fileKey);
        var filePath = Path.Combine(_storageDirectory, sanitizedKey);
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string fileKey, CancellationToken ct = default)
    {
        var sanitizedKey = Path.GetFileName(fileKey);
        var filePath = Path.Combine(_storageDirectory, sanitizedKey);
        return Task.FromResult(File.Exists(filePath));
    }
}
