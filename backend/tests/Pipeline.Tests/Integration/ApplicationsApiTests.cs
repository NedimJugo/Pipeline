using System;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pipeline.Application.Features.Applications.DTOs;
using Pipeline.Application.Features.Auth.DTOs;
using Pipeline.Domain.Enums;
using Pipeline.Infrastructure.Persistence;
using Xunit;

namespace Pipeline.Tests.Integration;

public class ApplicationsApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ApplicationsApiTests(WebApplicationFactory<Program> factory)
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
    public async Task ApplicationsFlow_CreateQueryUpdateStatusAndTimeline()
    {
        var dbName = Guid.NewGuid().ToString();
        var client = CreateAuthenticatedClient(dbName);

        // 1. Register a user to obtain JWT
        var registerResponse = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(
            Email: "apps_test@pipeline.local",
            Password: "Password12345!",
            DisplayName: "Applications Tester"));

        registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var authJson = await registerResponse.Content.ReadFromJsonAsync<JsonElement>();
        var token = authJson.GetProperty("accessToken").GetString();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // 2. Create Application
        var createResponse = await client.PostAsJsonAsync("/api/applications", new CreateApplicationRequest(
            RoleTitle: "Principal Architect",
            CompanyName: "OpenAI",
            Status: ApplicationStatus.Applied,
            WorkMode: WorkMode.Remote,
            SalaryMin: 180000,
            SalaryMax: 220000));

        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var createdApp = await createResponse.Content.ReadFromJsonAsync<ApplicationDetailDto>();
        createdApp.Should().NotBeNull();
        createdApp!.RoleTitle.Should().Be("Principal Architect");
        createdApp.CompanyName.Should().Be("OpenAI");
        createdApp.Status.Should().Be(ApplicationStatus.Applied);

        // 3. Query Applications List
        var listResponse = await client.GetAsync("/api/applications");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var list = await listResponse.Content.ReadFromJsonAsync<ApplicationListItemDto[]>();
        list.Should().NotBeNull();
        list!.Should().HaveCount(1);
        list[0].RoleTitle.Should().Be("Principal Architect");

        // 4. Update Status to Interview
        var statusResponse = await client.PatchAsJsonAsync($"/api/applications/{createdApp.Id}/status", new UpdateStatusRequest(
            Status: ApplicationStatus.Interview,
            Note: "Scheduled technical interview with hiring manager"));

        statusResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updatedApp = await statusResponse.Content.ReadFromJsonAsync<ApplicationDetailDto>();
        updatedApp!.Status.Should().Be(ApplicationStatus.Interview);

        // 5. Query Timeline
        var timelineResponse = await client.GetAsync($"/api/applications/{createdApp.Id}/timeline");
        timelineResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var timeline = await timelineResponse.Content.ReadFromJsonAsync<ApplicationTimelineItemDto[]>();
        timeline.Should().NotBeNull();
        timeline!.Should().HaveCountGreaterOrEqualTo(2); // Initial creation + status change
    }
}
