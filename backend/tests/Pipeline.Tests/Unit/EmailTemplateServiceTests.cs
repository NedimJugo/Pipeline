using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Pipeline.Application.Common.Interfaces;
using Pipeline.Application.Features.Templates.DTOs;
using Pipeline.Domain.Entities;
using Pipeline.Domain.Enums;
using Pipeline.Infrastructure.Persistence;
using Pipeline.Infrastructure.Services;
using Xunit;
using JobApplication = Pipeline.Domain.Entities.Application;

namespace Pipeline.Tests.Unit;

public class EmailTemplateServiceTests
{
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly PipelineDbContext _dbContext;

    public EmailTemplateServiceTests()
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
    public async Task EnsureDefaultTemplatesSeeded_CreatesSixSystemTemplates()
    {
        var service = new EmailTemplateService(_dbContext, _currentUserServiceMock.Object);

        await service.EnsureDefaultTemplatesSeededAsync(_userId);

        var templates = await service.GetTemplatesAsync();
        templates.Should().HaveCount(6);
        templates.Should().AllSatisfy(t => t.IsSystem.Should().BeTrue());
    }

    [Fact]
    public async Task RenderTemplate_SubstitutesPlaceholdersCorrectly()
    {
        var user = new User
        {
            Id = _userId,
            UserName = "testuser@example.com",
            Email = "testuser@example.com",
            DisplayName = "Jane Doe"
        };
        _dbContext.Users.Add(user);

        var company = new Company { Id = Guid.NewGuid(), Name = "Acme Corp" };
        _dbContext.Companies.Add(company);

        var app = new JobApplication
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            CompanyId = company.Id,
            Company = company,
            RoleTitle = "Staff Engineer",
            Status = ApplicationStatus.Applied
        };
        _dbContext.Applications.Add(app);

        var contact = new Contact
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            FullName = "John Recruiter",
            Role = "Tech Recruiter"
        };
        _dbContext.Contacts.Add(contact);
        await _dbContext.SaveChangesAsync();

        var service = new EmailTemplateService(_dbContext, _currentUserServiceMock.Object);
        var created = await service.CreateTemplateAsync(new CreateEmailTemplateRequest(
            Name: "Custom Follow Up",
            Subject: "Following up on {{role}} at {{company}}",
            Body: "Hi {{contactName}},\n\nI am writing to follow up on the {{role}} position at {{company}}.\n\nBest,\n{{myName}}",
            Category: EmailTemplateCategory.FollowUp));

        var rendered = await service.RenderTemplateAsync(created.Id, new RenderEmailTemplateRequest(app.Id, contact.Id));

        rendered.Subject.Should().Be("Following up on Staff Engineer at Acme Corp");
        rendered.Body.Should().Contain("Hi John Recruiter,");
        rendered.Body.Should().Contain("Staff Engineer position at Acme Corp");
        rendered.Body.Should().Contain("Jane Doe");
    }
}
