using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Pipeline.Application.Common.Interfaces;
using Pipeline.Application.Features.Contacts.DTOs;
using Pipeline.Application.Features.Interactions.DTOs;
using Pipeline.Domain.Entities;
using Pipeline.Domain.Enums;
using Pipeline.Infrastructure.Persistence;
using Pipeline.Infrastructure.Services;
using Xunit;
using JobApplication = Pipeline.Domain.Entities.Application;

namespace Pipeline.Tests.Unit;

public class ContactServiceTests
{
    private (PipelineDbContext db, Mock<ICurrentUserService> userMock, ContactService contactService, InteractionService interactionService, CompanyService companyService) CreateContext(Guid userId)
    {
        var userMock = new Mock<ICurrentUserService>();
        userMock.Setup(u => u.UserId).Returns(userId);
        userMock.Setup(u => u.IsAuthenticated).Returns(true);

        var options = new DbContextOptionsBuilder<PipelineDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var db = new PipelineDbContext(options, userMock.Object);
        var companyService = new CompanyService(db, userMock.Object);
        var contactService = new ContactService(db, userMock.Object, companyService);
        var interactionService = new InteractionService(db, userMock.Object);

        return (db, userMock, contactService, interactionService, companyService);
    }

    [Fact]
    public async Task CreateContact_WhenNeverContacted_HasColdWarmth()
    {
        var userId = Guid.NewGuid();
        var (db, _, contactService, _, _) = CreateContext(userId);

        var contact = await contactService.CreateContactAsync(new CreateContactRequest(
            FullName: "Sarah Connor",
            Role: "Talent Lead",
            CompanyName: "Cyberdyne Systems",
            Email: "sarah@cyberdyne.com",
            Type: ContactType.Recruiter));

        contact.Should().NotBeNull();
        contact.FullName.Should().Be("Sarah Connor");
        contact.CompanyName.Should().Be("Cyberdyne Systems");
        contact.Warmth.Should().Be(ContactWarmth.Cold);
        contact.LastContactedAt.Should().BeNull();
        contact.DaysSinceLastContact.Should().BeNull();
    }

    [Fact]
    public async Task LogInteraction_UpdatesLastContactedAt_AndTransitionsWarmthToHot()
    {
        var userId = Guid.NewGuid();
        var (db, _, contactService, interactionService, _) = CreateContext(userId);

        var contact = await contactService.CreateContactAsync(new CreateContactRequest(
            FullName: "John Doe",
            Role: "Engineering Director",
            Email: "john@tech.io"));

        // Log interaction today
        var interaction = await interactionService.LogInteractionAsync(new LogInteractionRequest(
            ContactId: contact.Id,
            Channel: InteractionChannel.LinkedIn,
            Direction: InteractionDirection.Outbound,
            Summary: "Sent connection request and intro note",
            FollowUpRequired: true,
            FollowUpDueAt: DateTime.UtcNow.AddDays(3)));

        interaction.Should().NotBeNull();
        interaction.ContactId.Should().Be(contact.Id);

        // Fetch contact and verify updated warmth
        var updated = await contactService.GetByIdAsync(contact.Id);
        updated.LastContactedAt.Should().NotBeNull();
        updated.Warmth.Should().Be(ContactWarmth.Hot);
        updated.DaysSinceLastContact.Should().Be(0);
        updated.NextFollowUpAt.Should().NotBeNull();
        updated.RecentInteractions.Should().HaveCount(1);
    }

    [Theory]
    [InlineData(2, ContactWarmth.Hot)]
    [InlineData(13, ContactWarmth.Hot)]
    [InlineData(14, ContactWarmth.Warm)]
    [InlineData(25, ContactWarmth.Warm)]
    [InlineData(31, ContactWarmth.Cooling)]
    [InlineData(60, ContactWarmth.Cooling)]
    [InlineData(61, ContactWarmth.Cold)]
    public async Task WarmthCalculation_CorrectlyEvaluatesDaysSinceLastContact(int daysAgo, ContactWarmth expectedWarmth)
    {
        var userId = Guid.NewGuid();
        var (db, _, contactService, _, _) = CreateContext(userId);

        var entity = new Contact
        {
            UserId = userId,
            FullName = "Test Person",
            LastContactedAt = DateTime.UtcNow.AddDays(-daysAgo)
        };
        db.Contacts.Add(entity);
        await db.SaveChangesAsync();

        var detail = await contactService.GetByIdAsync(entity.Id);
        detail.Warmth.Should().Be(expectedWarmth);
        detail.DaysSinceLastContact.Should().Be(daysAgo);
    }

    [Fact]
    public async Task LinkApplicationContact_AssociatesContactWithApplication()
    {
        var userId = Guid.NewGuid();
        var (db, _, contactService, _, _) = CreateContext(userId);

        var company = new Company { Name = "Tech Corp" };
        db.Companies.Add(company);
        await db.SaveChangesAsync();

        var contact = await contactService.CreateContactAsync(new CreateContactRequest(FullName: "Alex Hiring"));

        var app = new JobApplication
        {
            UserId = userId,
            CompanyId = company.Id,
            RoleTitle = "Staff Engineer",
            Status = ApplicationStatus.Interview
        };
        db.Applications.Add(app);
        await db.SaveChangesAsync();

        await contactService.LinkApplicationContactAsync(contact.Id, new LinkApplicationContactRequest(
            ApplicationId: app.Id,
            RoleInProcess: "Hiring Manager"));

        var linkedApps = await contactService.GetApplicationsForContactAsync(contact.Id);
        linkedApps.Should().HaveCount(1);
        linkedApps[0].ApplicationId.Should().Be(app.Id);
        linkedApps[0].RoleInProcess.Should().Be("Hiring Manager");
        linkedApps[0].CompanyName.Should().Be("Tech Corp");

        var appContacts = await contactService.GetContactsForApplicationAsync(app.Id);
        appContacts.Should().HaveCount(1);
        appContacts[0].Id.Should().Be(contact.Id);
    }

    [Fact]
    public async Task UserIsolation_ExcludesOtherUsersContacts()
    {
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();
        var (db, _, contactServiceA, _, _) = CreateContext(userA);

        // Add contact for user B directly
        var contactB = new Contact
        {
            UserId = userB,
            FullName = "Secret Contact of User B"
        };
        db.Contacts.Add(contactB);
        await db.SaveChangesAsync();

        // User A lists contacts
        var listA = await contactServiceA.GetContactsAsync();
        listA.Should().BeEmpty();

        // User A tries to get contact B by Id
        var act = async () => await contactServiceA.GetByIdAsync(contactB.Id);
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }
}
