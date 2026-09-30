using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Pipeline.Application.Common.Interfaces;
using Pipeline.Application.Features.Offers.DTOs;
using Pipeline.Domain.Entities;
using Pipeline.Domain.Enums;
using Pipeline.Infrastructure.Persistence;
using Pipeline.Infrastructure.Services;
using Xunit;
using JobApplication = Pipeline.Domain.Entities.Application;

namespace Pipeline.Tests.Unit;

public class OfferServiceTests
{
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly PipelineDbContext _dbContext;

    public OfferServiceTests()
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
    public async Task UpdateOfferDetails_UpdatesAllOfferFieldsAndComputesTotal()
    {
        var service = new OfferService(_dbContext, _currentUserServiceMock.Object);

        var company = new Company { Id = Guid.NewGuid(), Name = "Vercel" };
        var app = new JobApplication
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            CompanyId = company.Id,
            Company = company,
            RoleTitle = "Staff Frontend Architect",
            Status = ApplicationStatus.Offer
        };
        _dbContext.Companies.Add(company);
        _dbContext.Applications.Add(app);
        await _dbContext.SaveChangesAsync();

        var deadline = DateTime.UtcNow.AddDays(5);
        var request = new UpdateOfferDetailsRequest(
            OfferSalary: 195000m,
            OfferBonus: 25000m,
            OfferBenefits: "Unlimited PTO, 401k 5% match, $5k learning stipend",
            OfferDeadline: deadline,
            OfferNegotiationNotes: "Counter for 205k base or additional equity.");

        var result = await service.UpdateOfferDetailsAsync(app.Id, request);

        result.Should().NotBeNull();
        result.OfferSalary.Should().Be(195000m);
        result.OfferBonus.Should().Be(25000m);
        result.TotalCompensation.Should().Be(220000m);
        result.OfferBenefits.Should().Contain("Unlimited PTO");
        result.OfferNegotiationNotes.Should().Contain("Counter for 205k");
        result.DaysUntilDeadline.Should().Be(5);
    }

    [Fact]
    public async Task GetOfferComparison_ReturnsComparisonMatrixAndAvailableCriteria()
    {
        var service = new OfferService(_dbContext, _currentUserServiceMock.Object);

        var comp1 = new Company { Id = Guid.NewGuid(), Name = "Acme Cloud" };
        var app1 = new JobApplication
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            CompanyId = comp1.Id,
            Company = comp1,
            RoleTitle = "Senior Lead",
            Status = ApplicationStatus.Offer,
            OfferSalary = 175000m,
            OfferBonus = 15000m
        };

        var comp2 = new Company { Id = Guid.NewGuid(), Name = "Beta Labs" };
        var app2 = new JobApplication
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            CompanyId = comp2.Id,
            Company = comp2,
            RoleTitle = "Principal Engineer",
            Status = ApplicationStatus.Offer,
            OfferSalary = 210000m,
            OfferBonus = 30000m
        };

        _dbContext.Companies.AddRange(comp1, comp2);
        _dbContext.Applications.AddRange(app1, app2);
        await _dbContext.SaveChangesAsync();

        var view = await service.GetOfferComparisonAsync(new List<Guid> { app1.Id, app2.Id });

        view.Offers.Should().HaveCount(2);
        view.AvailableCriteria.Should().Contain("Compensation");
        view.AvailableCriteria.Should().Contain("Work-Life & Remote");
        view.Offers.Should().Contain(o => o.TotalCompensation == 240000m);
        view.Offers.Should().Contain(o => o.TotalCompensation == 190000m);
    }
}
