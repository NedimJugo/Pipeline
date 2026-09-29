using System;
using System.IO;
using System.Linq;

namespace Pipeline.Infrastructure.Services.Storage;

public static class FileValidationService
{
    public const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB

    private static readonly byte[] PdfMagic = { 0x25, 0x50, 0x44, 0x46 }; // %PDF
    private static readonly byte[] DocxMagic = { 0x50, 0x4B, 0x03, 0x04 }; // PK..
    private static readonly byte[] PngMagic = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
    private static readonly byte[] JpgMagic = { 0xFF, 0xD8, 0xFF };

    public static string ValidateAndNormalize(Stream stream, string fileName, string contentType, long? length = null)
    {
        if (stream == null)
            throw new ArgumentNullException(nameof(stream));

        if (length.HasValue && length.Value > MaxFileSizeBytes)
            throw new ArgumentException($"File size of {length.Value} bytes exceeds maximum allowed limit of {MaxFileSizeBytes} bytes (10 MB).");

        if (stream.CanSeek && stream.Length > MaxFileSizeBytes)
            throw new ArgumentException($"File size of {stream.Length} bytes exceeds maximum allowed limit of {MaxFileSizeBytes} bytes (10 MB).");

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(extension))
            throw new ArgumentException("File must have a valid extension (.pdf, .docx, .png, .jpg, .jpeg).");

        // Read magic bytes
        byte[] header = new byte[8];
        int bytesRead = stream.Read(header, 0, header.Length);
        if (stream.CanSeek)
        {
            stream.Position = 0; // Rewind for future reads
        }

        if (bytesRead < 3)
            throw new ArgumentException("File content is too short to be a valid document or image.");

        switch (extension)
        {
            case ".pdf":
                if (!header.Take(PdfMagic.Length).SequenceEqual(PdfMagic))
                    throw new ArgumentException("File content does not match standard PDF magic bytes.");
                return "application/pdf";

            case ".docx":
                if (!header.Take(DocxMagic.Length).SequenceEqual(DocxMagic))
                    throw new ArgumentException("File content does not match standard DOCX magic bytes.");
                return "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

            case ".png":
                if (bytesRead < PngMagic.Length || !header.Take(PngMagic.Length).SequenceEqual(PngMagic))
                    throw new ArgumentException("File content does not match standard PNG magic bytes.");
                return "image/png";

            case ".jpg":
            case ".jpeg":
                if (!header.Take(JpgMagic.Length).SequenceEqual(JpgMagic))
                    throw new ArgumentException("File content does not match standard JPEG magic bytes.");
                return "image/jpeg";

            default:
                throw new ArgumentException($"File extension '{extension}' is not supported. Allowed formats: PDF, DOCX, PNG, JPG.");
        }
    }
}
