using System;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pipeline.Application.Features.Applications.DTOs;
using Pipeline.Application.Features.Auth.DTOs;
using Pipeline.Application.Features.Contacts.DTOs;
using Pipeline.Application.Features.Interactions.DTOs;
using Pipeline.Domain.Enums;
using Pipeline.Infrastructure.Persistence;
using Xunit;

namespace Pipeline.Tests.Integration;

public class ContactsApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly WebApplicationFactory<Program> _factory;

    public ContactsApiTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private HttpClient CreateAuthenticatedClient(string dbName)
    {
        return _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("AUTO_MIGRATE", "false");
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<PipelineDbContext>));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                services.AddDbContext<PipelineDbContext>(options =>
                {
                    options.UseInMemoryDatabase(dbName);
                });
            });
        }).CreateClient();
    }

    [Fact]
    public async Task ContactsFlow_CreateInteractLinkAndTimelineIntegration()
    {
        var dbName = Guid.NewGuid().ToString();
        var client = CreateAuthenticatedClient(dbName);

        // 1. Register & Authenticate User
        var regResponse = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(
            Email: "contacts_test@pipeline.local",
            Password: "Password12345!",
            DisplayName: "Contact Tester"));
        var authJson = await regResponse.Content.ReadFromJsonAsync<JsonElement>();
        var token = authJson.GetProperty("accessToken").GetString();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // 2. Create an Application
        var appResponse = await client.PostAsJsonAsync("/api/applications", new CreateApplicationRequest(
            RoleTitle: "Lead DevOps Engineer",
            CompanyName: "CloudFlare",
            Status: ApplicationStatus.Applied), JsonOptions);
        var app = await appResponse.Content.ReadFromJsonAsync<ApplicationDetailDto>(JsonOptions);
        app.Should().NotBeNull();

        // 3. Create a Contact
        var contactResponse = await client.PostAsJsonAsync("/api/contacts", new CreateContactRequest(
            FullName: "Jane Recruiter",
            CompanyName: "CloudFlare",
            Role: "Senior Tech Recruiter",
            Email: "jane@cloudflare.com",
            Type: ContactType.Recruiter,
            ApplicationId: app!.Id,
            RoleInProcess: "Primary Recruiter"), JsonOptions);

        contactResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var contact = await contactResponse.Content.ReadFromJsonAsync<ContactDetailDto>(JsonOptions);
        contact.Should().NotBeNull();
        contact!.FullName.Should().Be("Jane Recruiter");
        contact.Warmth.Should().Be(ContactWarmth.Cold);
        contact.LinkedApplications.Should().HaveCount(1);
        contact.LinkedApplications[0].ApplicationId.Should().Be(app.Id);

        // 4. Log Interaction for Contact & Application
        var logResponse = await client.PostAsJsonAsync("/api/interactions", new LogInteractionRequest(
            ContactId: contact.Id,
            ApplicationId: app.Id,
            Channel: InteractionChannel.LinkedIn,
            Direction: InteractionDirection.Inbound,
            Summary: "Jane messaged on LinkedIn inviting to introductory call",
            FollowUpRequired: true,
            FollowUpDueAt: DateTime.UtcNow.AddDays(1)), JsonOptions);

        logResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var interaction = await logResponse.Content.ReadFromJsonAsync<InteractionDto>(JsonOptions);
        interaction.Should().NotBeNull();
        interaction!.Summary.Should().Contain("inviting to introductory call");

        // 5. Verify Contact Warmth Updated to Hot
        var contactGet = await client.GetAsync($"/api/contacts/{contact.Id}");
        var updatedContact = await contactGet.Content.ReadFromJsonAsync<ContactDetailDto>(JsonOptions);
        updatedContact!.Warmth.Should().Be(ContactWarmth.Hot);
        updatedContact.RecentInteractions.Should().HaveCount(1);

        // 6. Verify Interaction appears in Application Timeline
        var timelineResponse = await client.GetAsync($"/api/applications/{app.Id}/timeline");
        var timeline = await timelineResponse.Content.ReadFromJsonAsync<ApplicationTimelineItemDto[]>(JsonOptions);
        timeline.Should().NotBeNull();
        timeline.Should().Contain(t => t.Type == "Interaction" && t.Description!.Contains("inviting to introductory call"));

        // 7. Verify Application Contacts Endpoint
        var appContactsResponse = await client.GetAsync($"/api/applications/{app.Id}/contacts");
        var appContacts = await appContactsResponse.Content.ReadFromJsonAsync<ContactListItemDto[]>(JsonOptions);
        appContacts.Should().NotBeNull();
        appContacts!.Should().HaveCount(1);
        appContacts[0].FullName.Should().Be("Jane Recruiter");

        // 8. Update Contact
        var updateResponse = await client.PutAsJsonAsync($"/api/contacts/{contact.Id}", new UpdateContactRequest(
            FullName: "Jane Recruiter-Smith",
            CompanyName: "CloudFlare",
            Role: "Head of Talent Acquisition"), JsonOptions);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var modified = await updateResponse.Content.ReadFromJsonAsync<ContactDetailDto>(JsonOptions);
        modified!.FullName.Should().Be("Jane Recruiter-Smith");

        // 9. Delete Contact
        var deleteResponse = await client.DeleteAsync($"/api/contacts/{contact.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Confirm excluded from contact list
        var listResponse = await client.GetAsync("/api/contacts");
        var contactsList = await listResponse.Content.ReadFromJsonAsync<ContactListItemDto[]>(JsonOptions);
        contactsList!.Should().BeEmpty();
    }
}
