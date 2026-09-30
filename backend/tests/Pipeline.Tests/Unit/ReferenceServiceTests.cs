using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Pipeline.Application.Common.Interfaces;
using Pipeline.Application.Features.References.DTOs;
using Pipeline.Domain.Entities;
using Pipeline.Domain.Enums;
using Pipeline.Infrastructure.Persistence;
using Pipeline.Infrastructure.Services;
using Xunit;
using JobApplication = Pipeline.Domain.Entities.Application;

namespace Pipeline.Tests.Unit;

public class ReferenceServiceTests
{
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly PipelineDbContext _dbContext;

    public ReferenceServiceTests()
    {
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _currentUserServiceMock.Setup(s => s.UserId).Returns(_userId);
        _currentUserServiceMock.Setup(s => s.IsAuthenticated).Returns(true);

        var options = new DbContextOptionsBuilder<PipelineDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new PipelineDbContext(options, _currentUserServiceMock.Object);
    }

    [Fact]
    public async Task CreateReference_StoresAndReturnsReference()
    {
        var service = new ReferenceService(_dbContext, _currentUserServiceMock.Object);
        var request = new CreateReferenceRequest(
            FullName: "Sarah Connor",
            Relationship: "Former VP of Engineering",
            Email: "sarah@cyberdyne.io",
            Phone: "+1-555-0199",
            Company: "Cyberdyne Systems",
            Consent: ReferenceConsent.Agreed,
            Notes: "Can speak to distributed systems leadership.");

        var created = await service.CreateReferenceAsync(request);

        created.Should().NotBeNull();
        created.FullName.Should().Be("Sarah Connor");
        created.Consent.Should().Be(ReferenceConsent.Agreed);
        created.Company.Should().Be("Cyberdyne Systems");

        var list = await service.GetReferencesAsync();
        list.Should().HaveCount(1);
        list[0].FullName.Should().Be("Sarah Connor");
    }

    [Fact]
    public async Task ShareReference_WhenConsentNotAgreed_TriggersWarningUnlessOverridden()
    {
        var service = new ReferenceService(_dbContext, _currentUserServiceMock.Object);

        var refRecord = new JobReference
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            FullName = "John Doe",
            Relationship = "Colleague",
            Consent = ReferenceConsent.NotAsked
        };
        _dbContext.JobReferences.Add(refRecord);

        var company = new Company { Id = Guid.NewGuid(), Name = "Stripe" };
        var app = new JobApplication
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            CompanyId = company.Id,
            Company = company,
            RoleTitle = "Staff Backend Engineer"
        };
        _dbContext.Companies.Add(company);
        _dbContext.Applications.Add(app);
        await _dbContext.SaveChangesAsync();

        var res1 = await service.ShareReferenceAsync(refRecord.Id, new ShareReferenceRequest(app.Id, "Initial submission", false));
        res1.Success.Should().BeFalse();
        res1.WarningTriggered.Should().BeTrue();
        res1.WarningMessage.Should().Contain("NotAsked");

        var res2 = await service.ShareReferenceAsync(refRecord.Id, new ShareReferenceRequest(app.Id, "Initial submission", true));
        res2.Success.Should().BeTrue();
        res2.WarningTriggered.Should().BeFalse();
        res2.SharedRecord.Should().NotBeNull();
        res2.SharedRecord!.ApplicationId.Should().Be(app.Id);
    }

    [Fact]
    public async Task ShareReference_WhenConsentAgreed_SharesWithoutWarning()
    {
        var service = new ReferenceService(_dbContext, _currentUserServiceMock.Object);

        var refRecord = new JobReference
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            FullName = "Elena Rostova",
            Relationship = "Manager",
            Consent = ReferenceConsent.Agreed
        };
        _dbContext.JobReferences.Add(refRecord);

        var company = new Company { Id = Guid.NewGuid(), Name = "OpenAI" };
        var app = new JobApplication
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            CompanyId = company.Id,
            Company = company,
            RoleTitle = "Research Engineer"
        };
        _dbContext.Companies.Add(company);
        _dbContext.Applications.Add(app);
        await _dbContext.SaveChangesAsync();

        var res = await service.ShareReferenceAsync(refRecord.Id, new ShareReferenceRequest(app.Id, null, false));
        res.Success.Should().BeTrue();
        res.WarningTriggered.Should().BeFalse();

        var detail = await service.GetReferenceByIdAsync(refRecord.Id);
        detail.SharedApplications.Should().HaveCount(1);
        detail.SharedApplications[0].CompanyName.Should().Be("OpenAI");
    }

    [Fact]
    public async Task RecordNotification_UpdatesTimestamp()
    {
        var service = new ReferenceService(_dbContext, _currentUserServiceMock.Object);
        var refRecord = new JobReference
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            FullName = "Marcus Vance",
            Relationship = "Team Lead",
            Consent = ReferenceConsent.Agreed,
            LastNotifiedAt = null
        };
        _dbContext.JobReferences.Add(refRecord);
        await _dbContext.SaveChangesAsync();

        await service.RecordNotificationAsync(refRecord.Id);

        var detail = await service.GetReferenceByIdAsync(refRecord.Id);
        detail.LastNotifiedAt.Should().NotBeNull();
        detail.LastNotifiedAt.Value.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }
}
