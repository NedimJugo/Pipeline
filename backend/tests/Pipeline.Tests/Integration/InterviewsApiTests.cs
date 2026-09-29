using System;
using System.Collections.Generic;
using System.Linq;
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
using Pipeline.Application.Features.Interviews.DTOs;
using Pipeline.Domain.Enums;
using Pipeline.Infrastructure.Persistence;
using Xunit;

namespace Pipeline.Tests.Integration;

public class InterviewsApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly WebApplicationFactory<Program> _factory;

    public InterviewsApiTests(WebApplicationFactory<Program> factory)
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
    public async Task InterviewsFlow_FullLifecycle_PrepChecklist_Debrief_ICS_And_Isolation()
    {
        var dbName = Guid.NewGuid().ToString();
        var clientA = CreateAuthenticatedClient(dbName);

        // 1. Register User A
        var regA = await clientA.PostAsJsonAsync("/api/auth/register", new RegisterRequest(
            "user_a@pipeline.test", "Password123!", "User A"));
        regA.StatusCode.Should().Be(HttpStatusCode.OK);
        var authJsonA = await regA.Content.ReadFromJsonAsync<JsonElement>();
        var tokenA = authJsonA.GetProperty("accessToken").GetString();
        clientA.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);

        // 2. Create Application for User A
        var appResponse = await clientA.PostAsJsonAsync("/api/applications", new CreateApplicationRequest(
            RoleTitle: "Principal Site Reliability Engineer",
            CompanyName: "Netflix",
            Status: ApplicationStatus.Interview));
        appResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var app = await appResponse.Content.ReadFromJsonAsync<ApplicationDetailDto>(JsonOptions);
        app.Should().NotBeNull();

        // 3. Create Technical Interview via POST /api/interviews
        var scheduledTime = DateTime.UtcNow.AddDays(2);
        var createInterviewRes = await clientA.PostAsJsonAsync("/api/interviews", new CreateInterviewRequest(
            ApplicationId: app!.Id,
            Type: InterviewType.Technical,
            Format: InterviewFormat.Video,
            ScheduledAt: scheduledTime,
            DurationMinutes: 60,
            MeetingLink: "https://zoom.us/j/987654321"));

        createInterviewRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var interview = await createInterviewRes.Content.ReadFromJsonAsync<InterviewDetailDto>(JsonOptions);
        interview.Should().NotBeNull();
        interview!.Type.Should().Be(InterviewType.Technical);
        interview.Status.Should().Be(InterviewStatus.Scheduled);
        interview.PrepChecklist.Should().NotBeEmpty();
        interview.PrepChecklist.Should().Contain(i => i.Text.Contains("algorithms") || i.Text.Contains("architecture"));

        // 4. Update Prep Checklist (mark first item done)
        var updatedChecklist = interview.PrepChecklist
            .Select((item, idx) => idx == 0 ? item with { Done = true } : item)
            .ToList();

        var checklistRes = await clientA.PutAsJsonAsync($"/api/interviews/{interview.Id}/checklist",
            new UpdatePrepChecklistRequest(updatedChecklist));
        checklistRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var checklistDetail = await checklistRes.Content.ReadFromJsonAsync<InterviewDetailDto>(JsonOptions);
        checklistDetail!.PrepChecklist[0].Done.Should().BeTrue();

        // 5. Add Interview Question
        var addQRes = await clientA.PostAsJsonAsync($"/api/interviews/{interview.Id}/questions", new AddQuestionRequest(
            Question: "How do you handle cascading failures across distributed microservices?",
            MyAnswer: "Implement circuit breakers, rate limiting, and graceful degradation.",
            Category: InterviewQuestionCategory.Technical,
            Difficulty: 5,
            WasPrepared: true));
        addQRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var question = await addQRes.Content.ReadFromJsonAsync<InterviewQuestionDto>(JsonOptions);
        question.Should().NotBeNull();
        question!.Difficulty.Should().Be(5);

        // 6. Submit Post-Interview Debrief
        var debriefRes = await clientA.PatchAsJsonAsync($"/api/interviews/{interview.Id}/debrief", new UpdateDebriefRequest(
            SelfRating: 5,
            WentWell: "Clear explanation of circuit breaker patterns and bulkheads.",
            ToImprove: "Prepare more concise metrics examples.",
            ThankYouSent: true,
            OutcomeNotes: "Moved to final loop."));
        debriefRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var debriefDetail = await debriefRes.Content.ReadFromJsonAsync<InterviewDetailDto>(JsonOptions);
        debriefDetail!.SelfRating.Should().Be(5);
        debriefDetail.Status.Should().Be(InterviewStatus.Completed);
        debriefDetail.ThankYouSent.Should().BeTrue();

        // 7. Download Calendar .ics
        var icsRes = await clientA.GetAsync($"/api/interviews/{interview.Id}/calendar.ics");
        icsRes.StatusCode.Should().Be(HttpStatusCode.OK);
        icsRes.Content.Headers.ContentType!.MediaType.Should().Be("text/calendar");
        var icsText = await icsRes.Content.ReadAsStringAsync();
        icsText.Should().Contain("BEGIN:VCALENDAR");
        icsText.Should().Contain("SUMMARY:Technical Interview");

        // 8. Verify Application Interviews Endpoint
        var appInterviewsRes = await clientA.GetAsync($"/api/applications/{app.Id}/interviews");
        appInterviewsRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var appInterviews = await appInterviewsRes.Content.ReadFromJsonAsync<InterviewListItemDto[]>(JsonOptions);
        appInterviews.Should().NotBeNull();
        appInterviews!.Should().HaveCount(1);
        appInterviews[0].Id.Should().Be(interview.Id);

        // 9. Multi-Tenant Isolation: User B cannot access User A's interview
        var clientB = CreateAuthenticatedClient(dbName);
        var regB = await clientB.PostAsJsonAsync("/api/auth/register", new RegisterRequest(
            "user_b@pipeline.test", "Password123!", "User B"));
        var authJsonB = await regB.Content.ReadFromJsonAsync<JsonElement>();
        var tokenB = authJsonB.GetProperty("accessToken").GetString();
        clientB.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenB);

        var userBGetInterview = await clientB.GetAsync($"/api/interviews/{interview.Id}");
        userBGetInterview.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
