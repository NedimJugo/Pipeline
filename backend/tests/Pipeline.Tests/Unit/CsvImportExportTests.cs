using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using Pipeline.Application.Common.Interfaces;
using Pipeline.Application.Features.Companies.DTOs;
using Pipeline.Application.Features.Companies.Services;
using Pipeline.Domain.Entities;
using Pipeline.Domain.Enums;
using Pipeline.Infrastructure.Persistence;
using Pipeline.Infrastructure.Services;
using Xunit;
using JobApplication = Pipeline.Domain.Entities.Application;

namespace Pipeline.Tests.Unit;

public class CsvImportExportTests
{
    private readonly PipelineDbContext _dbContext;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly Mock<ICompanyService> _companyServiceMock;
    private readonly Guid _testUserId = Guid.NewGuid();

    public CsvImportExportTests()
    {
        var options = new DbContextOptionsBuilder<PipelineDbContext>()
            .UseInMemoryDatabase(databaseName: $"CsvTests_{Guid.NewGuid()}")
            .Options;

        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _currentUserServiceMock.Setup(s => s.UserId).Returns(_testUserId);

        _dbContext = new PipelineDbContext(options, _currentUserServiceMock.Object);

        _companyServiceMock = new Mock<ICompanyService>();
        _companyServiceMock.Setup(s => s.GetOrCreateCompanyAsync(It.IsAny<string>(), It.IsAny<string?>(), default))
            .ReturnsAsync((string name, string? web, System.Threading.CancellationToken ct) =>
            {
                var existing = _dbContext.Companies.FirstOrDefault(c => c.Name.ToLower() == name.ToLower());
                if (existing != null) return new CompanyDto(existing.Id, existing.Name, existing.Website, existing.Industry, existing.Size, existing.Location, existing.Notes, existing.LinkedInUrl);

                var newComp = new Company { Id = Guid.NewGuid(), Name = name, Website = web };
                _dbContext.Companies.Add(newComp);
                _dbContext.SaveChanges();
                return new CompanyDto(newComp.Id, newComp.Name, newComp.Website, newComp.Industry, newComp.Size, newComp.Location, newComp.Notes, newComp.LinkedInUrl);
            });
    }

    [Fact]
    public async Task ExportApplicationsCsvAsync_ReturnsValidCsvWithHeaders()
    {
        // Arrange
        var company = new Company { Id = Guid.NewGuid(), Name = "Acme Corp, Inc." };
        _dbContext.Companies.Add(company);

        var app = new JobApplication
        {
            Id = Guid.NewGuid(),
            UserId = _testUserId,
            CompanyId = company.Id,
            Company = company,
            RoleTitle = "Software \"Lead\" Engineer",
            Status = ApplicationStatus.Interview,
            Source = ApplicationSource.LinkedIn,
            WorkMode = WorkMode.Remote,
            EmploymentType = EmploymentType.FullTime,
            Location = "Remote, US",
            SalaryMin = 140000,
            SalaryMax = 170000,
            Currency = "USD",
            AppliedAt = new DateTime(2026, 9, 15, 0, 0, 0, DateTimeKind.Utc),
            ExcitementRating = 4,
            Priority = 2,
            Notes = "First round done"
        };
        _dbContext.Applications.Add(app);
        await _dbContext.SaveChangesAsync();

        var service = new ApplicationService(_dbContext, _currentUserServiceMock.Object, _companyServiceMock.Object);

        // Act
        var csvBytes = await service.ExportApplicationsCsvAsync();
        var csvText = Encoding.UTF8.GetString(csvBytes);

        // Assert
        Assert.Contains("CompanyName,RoleTitle,Status", csvText);
        Assert.Contains("\"Acme Corp, Inc.\"", csvText); // Quotes around commas
        Assert.Contains("\"Software \"\"Lead\"\" Engineer\"", csvText); // Escaped internal quotes
        Assert.Contains("Interview", csvText);
        Assert.Contains("140000", csvText);
    }

    [Fact]
    public async Task ImportApplicationsCsvAsync_CreatesNewApplicationsAndCompanies()
    {
        // Arrange
        var service = new ApplicationService(_dbContext, _currentUserServiceMock.Object, _companyServiceMock.Object);

        var csvContent =
            "CompanyName,RoleTitle,Status,Source,WorkMode,SalaryMin,SalaryMax,Location\n" +
            "Stripe,Staff Engineer,Interview,Referral,Remote,180000,220000,San Francisco\n" +
            "Datadog,Cloud Architect,Offer,LinkedIn,Hybrid,160000,190000,New York\n";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent));

        // Act
        var result = await service.ImportApplicationsCsvAsync(stream);

        // Assert
        Assert.Equal(2, result.TotalProcessed);
        Assert.Equal(2, result.CreatedCount);
        Assert.Equal(0, result.FailedCount);

        var stripeApp = await _dbContext.Applications
            .Include(a => a.Company)
            .FirstOrDefaultAsync(a => a.RoleTitle == "Staff Engineer");

        Assert.NotNull(stripeApp);
        Assert.Equal("Stripe", stripeApp.Company?.Name);
        Assert.Equal(ApplicationStatus.Interview, stripeApp.Status);
        Assert.Equal(ApplicationSource.Referral, stripeApp.Source);
        Assert.Equal(180000, stripeApp.SalaryMin);
    }

    [Fact]
    public async Task ImportApplicationsCsvAsync_UpdatesExistingApplication()
    {
        // Arrange
        var company = new Company { Id = Guid.NewGuid(), Name = "Vercel" };
        _dbContext.Companies.Add(company);

        var existing = new JobApplication
        {
            Id = Guid.NewGuid(),
            UserId = _testUserId,
            CompanyId = company.Id,
            RoleTitle = "Frontend Architect",
            Status = ApplicationStatus.Screening,
            SalaryMin = 150000
        };
        _dbContext.Applications.Add(existing);
        await _dbContext.SaveChangesAsync();

        var service = new ApplicationService(_dbContext, _currentUserServiceMock.Object, _companyServiceMock.Object);

        var csvContent =
            "CompanyName,RoleTitle,Status,SalaryMin,SalaryMax\n" +
            "Vercel,Frontend Architect,Offer,175000,200000\n";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent));

        // Act
        var result = await service.ImportApplicationsCsvAsync(stream);

        // Assert
        Assert.Equal(1, result.TotalProcessed);
        Assert.Equal(0, result.CreatedCount);
        Assert.Equal(1, result.UpdatedCount);
        Assert.Equal(0, result.FailedCount);

        var updated = await _dbContext.Applications.FindAsync(existing.Id);
        Assert.Equal(ApplicationStatus.Offer, updated?.Status);
        Assert.Equal(175000, updated?.SalaryMin);
    }

    [Fact]
    public async Task ImportApplicationsCsvAsync_RecordsErrorForMissingRequiredFields()
    {
        // Arrange
        var service = new ApplicationService(_dbContext, _currentUserServiceMock.Object, _companyServiceMock.Object);

        var csvContent =
            "CompanyName,RoleTitle,Status\n" +
            ",Staff Engineer,Applied\n" + // Missing CompanyName
            "Datadog,,Screening\n";       // Missing RoleTitle

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent));

        // Act
        var result = await service.ImportApplicationsCsvAsync(stream);

        // Assert
        Assert.Equal(2, result.TotalProcessed);
        Assert.Equal(0, result.CreatedCount);
        Assert.Equal(2, result.FailedCount);
        Assert.Equal(2, result.Errors.Count);
    }
}
