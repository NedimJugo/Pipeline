using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Pipeline.Application.Common.Interfaces;

public record FileDownloadResult(Stream Stream, string ContentType, string FileName);

public interface IFileStorage
{
    Task<string> UploadAsync(Stream stream, string fileName, string contentType, CancellationToken ct = default);
    Task<FileDownloadResult> DownloadAsync(string fileKey, CancellationToken ct = default);
    Task<string> GetPresignedDownloadUrlAsync(string fileKey, string fileName, TimeSpan expiry, CancellationToken ct = default);
    Task DeleteAsync(string fileKey, CancellationToken ct = default);
    Task<bool> ExistsAsync(string fileKey, CancellationToken ct = default);
}
