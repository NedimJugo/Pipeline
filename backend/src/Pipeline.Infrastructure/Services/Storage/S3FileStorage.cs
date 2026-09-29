using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using Pipeline.Application.Common.Interfaces;

namespace Pipeline.Infrastructure.Services.Storage;

public class S3FileStorage : IFileStorage
{
    private readonly IAmazonS3 _s3Client;
    private readonly string _bucketName;

    public S3FileStorage(IAmazonS3 s3Client, string bucketName)
    {
        _s3Client = s3Client ?? throw new ArgumentNullException(nameof(s3Client));
        _bucketName = string.IsNullOrWhiteSpace(bucketName) ? "pipeline" : bucketName;
    }

    public async Task<string> UploadAsync(Stream stream, string fileName, string contentType, CancellationToken ct = default)
    {
        var validatedContentType = FileValidationService.ValidateAndNormalize(stream, fileName, contentType);

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        var fileKey = $"documents/{Guid.NewGuid():N}{extension}";

        if (stream.CanSeek)
        {
            stream.Position = 0;
        }

        var putRequest = new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = fileKey,
            InputStream = stream,
            ContentType = validatedContentType,
            AutoCloseStream = false
        };

        await _s3Client.PutObjectAsync(putRequest, ct);

        return fileKey;
    }

    public async Task<FileDownloadResult> DownloadAsync(string fileKey, CancellationToken ct = default)
    {
        var getRequest = new GetObjectRequest
        {
            BucketName = _bucketName,
            Key = fileKey
        };

        try
        {
            var response = await _s3Client.GetObjectAsync(getRequest, ct);
            var memoryStream = new MemoryStream();
            await response.ResponseStream.CopyToAsync(memoryStream, ct);
            memoryStream.Position = 0;

            return new FileDownloadResult(
                memoryStream,
                response.Headers.ContentType ?? "application/octet-stream",
                Path.GetFileName(fileKey));
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            throw new FileNotFoundException($"Object with key '{fileKey}' not found in bucket '{_bucketName}'.", ex);
        }
    }

    public Task<string> GetPresignedDownloadUrlAsync(string fileKey, string fileName, TimeSpan expiry, CancellationToken ct = default)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = _bucketName,
            Key = fileKey,
            Expires = DateTime.UtcNow.Add(expiry),
            Verb = HttpVerb.GET,
            ResponseHeaderOverrides = new ResponseHeaderOverrides
            {
                ContentDisposition = $"inline; filename=\"{Uri.EscapeDataString(fileName)}\""
            }
        };

        var url = _s3Client.GetPreSignedURL(request);
        return Task.FromResult(url);
    }

    public async Task DeleteAsync(string fileKey, CancellationToken ct = default)
    {
        var deleteRequest = new DeleteObjectRequest
        {
            BucketName = _bucketName,
            Key = fileKey
        };

        await _s3Client.DeleteObjectAsync(deleteRequest, ct);
    }

    public async Task<bool> ExistsAsync(string fileKey, CancellationToken ct = default)
    {
        try
        {
            await _s3Client.GetObjectMetadataAsync(_bucketName, fileKey, ct);
            return true;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
    }
}
