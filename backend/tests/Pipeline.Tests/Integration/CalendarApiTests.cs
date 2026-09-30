using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
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
using Pipeline.Application.Features.Calendar.DTOs;
using Pipeline.Application.Features.Interviews.DTOs;
using Pipeline.Application.Features.Offers.DTOs;
using Pipeline.Application.Features.Tasks.DTOs;
using Pipeline.Domain.Enums;
using Pipeline.Infrastructure.Persistence;
using Xunit;

namespace Pipeline.Tests.Integration;

public class CalendarApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly WebApplicationFactory<Program> _factory;

    public CalendarApiTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private HttpClient CreateClient(string dbName)
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

    private async Task<string> RegisterAndGetTokenAsync(HttpClient client, string email)
    {
        var registerRequest = new RegisterRequest(email, "Password123!", "Calendar Test User");
        var res = await client.PostAsJsonAsync("/api/auth/register", registerRequest, JsonOptions);
        res.StatusCode.Should().Be(HttpStatusCode.OK);

        var authRes = await res.Content.ReadFromJsonAsync<AuthResult>(JsonOptions);
        authRes.Should().NotBeNull();
        return authRes!.AccessToken;
    }

    [Fact]
    public async Task CalendarApi_EventsQueryingAndIcsFeedSubscription_WorksEndToEnd()
    {
        var dbName = Guid.NewGuid().ToString();
        var clientUser1 = CreateClient(dbName);
        var clientUser2 = CreateClient(dbName);

        var token1 = await RegisterAndGetTokenAsync(clientUser1, "user1.cal@example.com");
        var token2 = await RegisterAndGetTokenAsync(clientUser2, "user2.cal@example.com");

        clientUser1.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token1);
        clientUser2.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token2);

        // 1. User 1 creates an application with OfferDeadline
        var deadline = DateTime.UtcNow.AddDays(7);
        var appRes = await clientUser1.PostAsJsonAsync("/api/applications", new CreateApplicationRequest(
            CompanyName: "GitHub",
            RoleTitle: "Staff Platform Engineer",
            Status: ApplicationStatus.Offer
        ), JsonOptions);
        appRes.EnsureSuccessStatusCode();
        var app = await appRes.Content.ReadFromJsonAsync<ApplicationDetailDto>(JsonOptions);

        // Record offer details including OfferDeadline
        await clientUser1.PutAsJsonAsync($"/api/offers/application/{app!.Id}", new UpdateOfferDetailsRequest(
            OfferSalary: 195000m,
            OfferBonus: 25000m,
            OfferDeadline: deadline
        ), JsonOptions);

        // 2. User 1 creates an interview
        var interviewRes = await clientUser1.PostAsJsonAsync("/api/interviews", new CreateInterviewRequest(
            ApplicationId: app!.Id,
            Type: InterviewType.Technical,
            ScheduledAt: DateTime.UtcNow.AddDays(2),
            DurationMinutes: 60,
            Location: "https://zoom.us/j/999"
        ), JsonOptions);
        interviewRes.EnsureSuccessStatusCode();

        // 3. User 1 creates a task
        var taskRes = await clientUser1.PostAsJsonAsync("/api/tasks", new CreateTaskRequest(
            Title: "Review compensation benchmark",
            DueAt: DateTime.UtcNow.AddDays(3),
            ApplicationId: app.Id
        ), JsonOptions);
        taskRes.EnsureSuccessStatusCode();

        // 4. Query Events for User 1
        var eventsRes = await clientUser1.GetAsync("/api/calendar");
        eventsRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var events = await eventsRes.Content.ReadFromJsonAsync<List<CalendarEventDto>>(JsonOptions);
        events.Should().NotBeNull();
        events!.Should().HaveCount(3);
        events.Should().Contain(e => e.Type == CalendarEventType.Interview);
        events.Should().Contain(e => e.Type == CalendarEventType.Task);
        events.Should().Contain(e => e.Type == CalendarEventType.OfferDeadline);

        // 5. Query Events for User 2 (Multi-tenant check)
        var eventsRes2 = await clientUser2.GetAsync("/api/calendar");
        eventsRes2.StatusCode.Should().Be(HttpStatusCode.OK);
        var events2 = await eventsRes2.Content.ReadFromJsonAsync<List<CalendarEventDto>>(JsonOptions);
        events2.Should().BeEmpty();

        // 6. Get Feed URL for User 1
        var feedUrlRes = await clientUser1.GetAsync("/api/calendar/feed-url");
        feedUrlRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var feedDto = await feedUrlRes.Content.ReadFromJsonAsync<CalendarFeedUrlDto>(JsonOptions);
        feedDto.Should().NotBeNull();
        feedDto!.Token.Should().NotBeNullOrWhiteSpace();

        // 7. Download ICS feed anonymously (without auth headers)
        var anonymousClient = CreateClient(dbName);
        var icsRes = await anonymousClient.GetAsync($"/api/calendar/feed/{feedDto.Token}.ics");
        icsRes.StatusCode.Should().Be(HttpStatusCode.OK);
        icsRes.Content.Headers.ContentType!.MediaType.Should().Be("text/calendar");

        var icsText = await icsRes.Content.ReadAsStringAsync();
        icsText.Should().Contain("BEGIN:VCALENDAR");
        icsText.Should().Contain("Staff Platform Engineer @ GitHub");
        icsText.Should().Contain("Review compensation benchmark");
        icsText.Should().Contain("END:VCALENDAR");

        // 8. Rotate token
        var rotateRes = await clientUser1.PostAsync("/api/calendar/rotate-token", null);
        rotateRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var rotatedDto = await rotateRes.Content.ReadFromJsonAsync<CalendarFeedUrlDto>(JsonOptions);
        rotatedDto.Should().NotBeNull();
        rotatedDto!.Token.Should().NotBe(feedDto.Token);

        // Old token now returns 404
        var oldIcsRes = await anonymousClient.GetAsync($"/api/calendar/feed/{feedDto.Token}.ics");
        oldIcsRes.StatusCode.Should().Be(HttpStatusCode.NotFound);

        // New token returns 200
        var newIcsRes = await anonymousClient.GetAsync($"/api/calendar/feed/{rotatedDto.Token}.ics");
        newIcsRes.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
