using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Pipeline.Infrastructure.Services.Storage;
using Xunit;

namespace Pipeline.Tests.Unit;

public class FileStorageTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly LocalFileStorage _storage;

    public FileStorageTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "pipeline_storage_tests_" + Guid.NewGuid().ToString("N"));
        _storage = new LocalFileStorage(_tempDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            try
            {
                Directory.Delete(_tempDirectory, true);
            }
            catch
            {
                // Best effort cleanup
            }
        }
    }

    private static MemoryStream CreateValidPdfStream()
    {
        var bytes = Encoding.UTF8.GetBytes("%PDF-1.4 Fake PDF Header Content For Testing Only");
        return new MemoryStream(bytes);
    }

    private static MemoryStream CreateValidDocxStream()
    {
        var header = new byte[] { 0x50, 0x4B, 0x03, 0x04, 0x00, 0x00, 0x00, 0x00 };
        return new MemoryStream(header);
    }

    private static MemoryStream CreateValidPngStream()
    {
        var header = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00 };
        return new MemoryStream(header);
    }

    private static MemoryStream CreateValidJpgStream()
    {
        var header = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46 };
        return new MemoryStream(header);
    }

    [Fact]
    public async Task Upload_ValidPdf_SucceedsAndStoresFile()
    {
        using var stream = CreateValidPdfStream();
        var key = await _storage.UploadAsync(stream, "resume_v1.pdf", "application/pdf");

        Assert.NotNull(key);
        Assert.EndsWith(".pdf", key);

        var exists = await _storage.ExistsAsync(key);
        Assert.True(exists);

        var download = await _storage.DownloadAsync(key);
        Assert.Equal("application/pdf", download.ContentType);
        Assert.NotNull(download.Stream);
    }

    [Fact]
    public async Task Upload_ValidDocx_Succeeds()
    {
        using var stream = CreateValidDocxStream();
        var key = await _storage.UploadAsync(stream, "cover_letter.docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document");

        Assert.NotNull(key);
        Assert.EndsWith(".docx", key);
        Assert.True(await _storage.ExistsAsync(key));
    }

    [Fact]
    public async Task Upload_ValidImages_Succeed()
    {
        using var pngStream = CreateValidPngStream();
        var pngKey = await _storage.UploadAsync(pngStream, "screenshot.png", "image/png");
        Assert.EndsWith(".png", pngKey);

        using var jpgStream = CreateValidJpgStream();
        var jpgKey = await _storage.UploadAsync(jpgStream, "photo.jpg", "image/jpeg");
        Assert.EndsWith(".jpg", jpgKey);
    }

    [Fact]
    public async Task Upload_FakePdfWithTextContent_ThrowsArgumentException()
    {
        var fakeBytes = Encoding.UTF8.GetBytes("This is not a PDF, it is plain text disguised as PDF");
        using var stream = new MemoryStream(fakeBytes);

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            _storage.UploadAsync(stream, "exploit.pdf", "application/pdf"));

        Assert.Contains("magic bytes", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Upload_UnsupportedExtension_ThrowsArgumentException()
    {
        var bytes = Encoding.UTF8.GetBytes("echo 'malicious'");
        using var stream = new MemoryStream(bytes);

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            _storage.UploadAsync(stream, "script.sh", "application/x-sh"));

        Assert.Contains("not supported", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Upload_Exceeds10MbLimit_ThrowsArgumentException()
    {
        using var stream = CreateValidPdfStream();
        long oversized = 11 * 1024 * 1024; // 11MB

        var ex = Assert.Throws<ArgumentException>(() =>
            FileValidationService.ValidateAndNormalize(stream, "huge.pdf", "application/pdf", oversized));

        Assert.Contains("exceeds maximum allowed limit", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Delete_RemovesFileFromStorage()
    {
        using var stream = CreateValidPdfStream();
        var key = await _storage.UploadAsync(stream, "delete_me.pdf", "application/pdf");

        Assert.True(await _storage.ExistsAsync(key));

        await _storage.DeleteAsync(key);
        Assert.False(await _storage.ExistsAsync(key));
    }

    [Fact]
    public async Task GetPresignedDownloadUrl_ReturnsStreamUrl()
    {
        using var stream = CreateValidPdfStream();
        var key = await _storage.UploadAsync(stream, "my_cv.pdf", "application/pdf");

        var url = await _storage.GetPresignedDownloadUrlAsync(key, "my_cv.pdf", TimeSpan.FromMinutes(30));
        Assert.NotNull(url);
        Assert.Contains(Uri.EscapeDataString(key), url);
    }
}
