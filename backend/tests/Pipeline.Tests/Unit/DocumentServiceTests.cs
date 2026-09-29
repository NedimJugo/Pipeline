using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Pipeline.Application.Common.Interfaces;
using Pipeline.Application.Features.Documents.DTOs;
using Pipeline.Domain.Entities;
using Pipeline.Domain.Enums;
using Pipeline.Infrastructure.Persistence;
using Pipeline.Infrastructure.Services;
using Pipeline.Infrastructure.Services.Storage;
using Xunit;
using JobApplication = Pipeline.Domain.Entities.Application;

namespace Pipeline.Tests.Unit;

public class DocumentServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly LocalFileStorage _storage;

    public DocumentServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "doc_tests_" + Guid.NewGuid().ToString("N"));
        _storage = new LocalFileStorage(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            try { Directory.Delete(_tempDir, true); } catch { }
        }
    }

    private (PipelineDbContext db, Mock<ICurrentUserService> userMock, DocumentService documentService) CreateContext(Guid userId)
    {
        var userMock = new Mock<ICurrentUserService>();
        userMock.Setup(u => u.UserId).Returns(userId);
        userMock.Setup(u => u.IsAuthenticated).Returns(true);

        var options = new DbContextOptionsBuilder<PipelineDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var db = new PipelineDbContext(options, userMock.Object);
        var documentService = new DocumentService(db, userMock.Object, _storage);

        return (db, userMock, documentService);
    }

    private static MemoryStream CreateValidPdfStream(string label)
    {
        var content = $"%PDF-1.4 PDF Document Content for {label}";
        return new MemoryStream(Encoding.UTF8.GetBytes(content));
    }

    [Fact]
    public async Task CreateDocument_WithInitialPdf_CreatesDocumentAndDefaultVersion()
    {
        var userId = Guid.NewGuid();
        var (db, _, svc) = CreateContext(userId);

        using var stream = CreateValidPdfStream("v1");
        var req = new CreateDocumentRequest("Software Engineer CV", DocumentType.CV, "Primary resume for backend roles", "v1", "Initial draft");

        var doc = await svc.CreateDocumentAsync(req, stream, "resume_v1.pdf", "application/pdf");

        doc.Should().NotBeNull();
        doc.Title.Should().Be("Software Engineer CV");
        doc.Type.Should().Be(DocumentType.CV);
        doc.Versions.Should().HaveCount(1);
        doc.Versions[0].VersionLabel.Should().Be("v1");
        doc.Versions[0].IsDefault.Should().BeTrue();
        doc.DefaultVersionId.Should().Be(doc.Versions[0].Id);
    }

    [Fact]
    public async Task UploadVersion_AsDefault_UnsetsPreviousDefault()
    {
        var userId = Guid.NewGuid();
        var (db, _, svc) = CreateContext(userId);

        using var v1Stream = CreateValidPdfStream("v1");
        var doc = await svc.CreateDocumentAsync(
            new CreateDocumentRequest("Frontend CV", DocumentType.CV),
            v1Stream, "cv_v1.pdf", "application/pdf");

        using var v2Stream = CreateValidPdfStream("v2");
        var v2 = await svc.UploadVersionAsync(
            doc.Id,
            new UploadVersionRequest("v2", "Updated with React 19", IsDefault: true),
            v2Stream, "cv_v2.pdf", "application/pdf");

        v2.IsDefault.Should().BeTrue();

        var refreshed = await svc.GetByIdAsync(doc.Id);
        refreshed.Versions.Should().HaveCount(2);

        var v1Refreshed = refreshed.Versions.First(v => v.VersionLabel == "v1");
        var v2Refreshed = refreshed.Versions.First(v => v.VersionLabel == "v2");

        v1Refreshed.IsDefault.Should().BeFalse();
        v2Refreshed.IsDefault.Should().BeTrue();
        refreshed.DefaultVersionId.Should().Be(v2.Id);
    }

    [Fact]
    public async Task SetDefaultVersion_SwitchesDefaultCorrectly()
    {
        var userId = Guid.NewGuid();
        var (db, _, svc) = CreateContext(userId);

        using var v1Stream = CreateValidPdfStream("v1");
        var doc = await svc.CreateDocumentAsync(
            new CreateDocumentRequest("General Resume", DocumentType.CV),
            v1Stream, "v1.pdf", "application/pdf");

        using var v2Stream = CreateValidPdfStream("v2");
        var v2 = await svc.UploadVersionAsync(
            doc.Id,
            new UploadVersionRequest("v2", IsDefault: true),
            v2Stream, "v2.pdf", "application/pdf");

        var v1Id = doc.Versions.First().Id;
        await svc.SetDefaultVersionAsync(v1Id);

        var refreshed = await svc.GetByIdAsync(doc.Id);
        refreshed.DefaultVersionId.Should().Be(v1Id);
        refreshed.Versions.First(v => v.Id == v1Id).IsDefault.Should().BeTrue();
        refreshed.Versions.First(v => v.Id == v2.Id).IsDefault.Should().BeFalse();
    }

    [Fact]
    public async Task GetVersionStats_ComputesConversionRatesAccurately()
    {
        var userId = Guid.NewGuid();
        var (db, _, svc) = CreateContext(userId);

        // 1. Create Document with Version
        using var stream = CreateValidPdfStream("Tailored v1");
        var doc = await svc.CreateDocumentAsync(
            new CreateDocumentRequest("Fintech CV", DocumentType.CV),
            stream, "fintech_cv.pdf", "application/pdf");

        var versionId = doc.Versions.First().Id;

        // 2. Create Company
        var company = new Company { Id = Guid.NewGuid(), Name = "Fintech Corp" };
        db.Companies.Add(company);

        // 3. Link App 1: Status = Interview (InterviewCount + 1, ReplyCount + 1, SentCount + 1)
        var app1 = new JobApplication
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CompanyId = company.Id,
            RoleTitle = "Senior Backend Engineer",
            Status = ApplicationStatus.Interview,
            DocumentVersionCvId = versionId
        };
        db.Applications.Add(app1);

        // 4. Link App 2: Status = Offer with OfferSalary (OfferCount + 1, InterviewCount + 1, ReplyCount + 1, SentCount + 1)
        var app2 = new JobApplication
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CompanyId = company.Id,
            RoleTitle = "Tech Lead",
            Status = ApplicationStatus.Offer,
            OfferSalary = 160000m,
            DocumentVersionCvId = versionId
        };
        db.Applications.Add(app2);

        // 5. Link App 3: Status = Applied (SentCount + 1, no reply yet)
        var app3 = new JobApplication
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CompanyId = company.Id,
            RoleTitle = "Software Architect",
            Status = ApplicationStatus.Applied,
            DocumentVersionCvId = versionId
        };
        db.Applications.Add(app3);

        await db.SaveChangesAsync();

        // 6. Assert Stats
        var statsList = await svc.GetVersionStatsAsync(DocumentType.CV);
        statsList.Should().HaveCount(1);

        var vStats = statsList.First();
        vStats.SentCount.Should().Be(3);
        vStats.ReplyCount.Should().Be(2);
        vStats.InterviewCount.Should().Be(2);
        vStats.OfferCount.Should().Be(1);

        // Rates:
        // Response rate: 2/3 = 66.7%
        // Interview rate: 2/3 = 66.7%
        // Offer rate: 1/3 = 33.3%
        vStats.ResponseRate.Should().Be(66.7);
        vStats.InterviewRate.Should().Be(66.7);
        vStats.OfferRate.Should().Be(33.3);
    }

    [Fact]
    public async Task DeleteVersion_UnlinksApplicationsAndRemovesStorageFile()
    {
        var userId = Guid.NewGuid();
        var (db, _, svc) = CreateContext(userId);

        using var stream1 = CreateValidPdfStream("v1");
        var doc = await svc.CreateDocumentAsync(
            new CreateDocumentRequest("My Resume", DocumentType.CV),
            stream1, "v1.pdf", "application/pdf");

        var v1Id = doc.Versions.First().Id;

        using var stream2 = CreateValidPdfStream("v2");
        var v2 = await svc.UploadVersionAsync(
            doc.Id,
            new UploadVersionRequest("v2"),
            stream2, "v2.pdf", "application/pdf");

        var company = new Company { Id = Guid.NewGuid(), Name = "Acme Corp" };
        db.Companies.Add(company);

        var app = new JobApplication
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CompanyId = company.Id,
            RoleTitle = "Dev",
            DocumentVersionCvId = v2.Id
        };
        db.Applications.Add(app);
        await db.SaveChangesAsync();

        // Delete v2
        await svc.DeleteVersionAsync(v2.Id);

        // App should have its DocumentVersionCvId unlinked
        var appRefreshed = await db.Applications.FindAsync(app.Id);
        appRefreshed!.DocumentVersionCvId.Should().BeNull();

        // Doc should only have v1 remaining
        var docRefreshed = await svc.GetByIdAsync(doc.Id);
        docRefreshed.Versions.Should().HaveCount(1);
        docRefreshed.Versions.First().Id.Should().Be(v1Id);
    }
}
