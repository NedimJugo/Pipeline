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
using Pipeline.Domain.Enums;
using Pipeline.Infrastructure.Persistence;
using Xunit;

namespace Pipeline.Tests.Integration;

public class ApplicationsApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

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
        var createdApp = await createResponse.Content.ReadFromJsonAsync<ApplicationDetailDto>(JsonOptions);
        createdApp.Should().NotBeNull();
        createdApp!.RoleTitle.Should().Be("Principal Architect");
        createdApp.CompanyName.Should().Be("OpenAI");
        createdApp.Status.Should().Be(ApplicationStatus.Applied);

        // 3. Query Applications List
        var listResponse = await client.GetAsync("/api/applications");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var list = await listResponse.Content.ReadFromJsonAsync<ApplicationListItemDto[]>(JsonOptions);
        list.Should().NotBeNull();
        list!.Should().HaveCount(1);
        list[0].RoleTitle.Should().Be("Principal Architect");

        // 4. Update Status to Interview
        var statusResponse = await client.PatchAsJsonAsync($"/api/applications/{createdApp.Id}/status", new UpdateStatusRequest(
            Status: ApplicationStatus.Interview,
            Note: "Scheduled technical interview with hiring manager"), JsonOptions);

        statusResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updatedApp = await statusResponse.Content.ReadFromJsonAsync<ApplicationDetailDto>(JsonOptions);
        updatedApp!.Status.Should().Be(ApplicationStatus.Interview);

        // 5. Query Timeline
        var timelineResponse = await client.GetAsync($"/api/applications/{createdApp.Id}/timeline");
        timelineResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var timeline = await timelineResponse.Content.ReadFromJsonAsync<ApplicationTimelineItemDto[]>(JsonOptions);
        timeline.Should().NotBeNull();
        timeline!.Should().HaveCountGreaterOrEqualTo(2); // Initial creation + status change
    }

    [Fact]
    public async Task ApplicationsFlow_UpdateDuplicateSearchDelete()
    {
        var dbName = Guid.NewGuid().ToString();
        var client = CreateAuthenticatedClient(dbName);

        // 1. Register & Authenticate
        var regRes = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(
            Email: "flow2_test@pipeline.local",
            Password: "Password12345!",
            DisplayName: "Flow Tester"));
        var authJson = await regRes.Content.ReadFromJsonAsync<JsonElement>();
        var token = authJson.GetProperty("accessToken").GetString();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // 2. Create Application
        var createRes = await client.PostAsJsonAsync("/api/applications", new CreateApplicationRequest(
            RoleTitle: "Staff Engineer",
            CompanyName: "Datadog",
            JobDescription: "Kubernetes, Go, and high-throughput telemetry pipelines",
            SalaryMin: 200000,
            SalaryMax: 250000), JsonOptions);
        var created = await createRes.Content.ReadFromJsonAsync<ApplicationDetailDto>(JsonOptions);
        created.Should().NotBeNull();

        // 3. Search query
        var searchRes = await client.GetAsync("/api/search?q=telemetry");
        searchRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var searchJson = await searchRes.Content.ReadFromJsonAsync<JsonElement>();
        searchJson.GetProperty("applications").GetArrayLength().Should().Be(1);

        // 4. Update Application
        var updateRes = await client.PutAsJsonAsync($"/api/applications/{created!.Id}", new CreateApplicationRequest(
            RoleTitle: "Principal Staff Engineer",
            CompanyName: "Datadog",
            Notes: "Reviewed company engineering blog"), JsonOptions);
        updateRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await updateRes.Content.ReadFromJsonAsync<ApplicationDetailDto>(JsonOptions);
        updated!.RoleTitle.Should().Be("Principal Staff Engineer");

        // 5. Duplicate Application
        var dupRes = await client.PostAsync($"/api/applications/{created.Id}/duplicate", null);
        dupRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var duplicated = await dupRes.Content.ReadFromJsonAsync<ApplicationDetailDto>(JsonOptions);
        duplicated!.Id.Should().NotBe(created.Id);
        duplicated.RoleTitle.Should().Contain("Principal Staff Engineer");

        // 6. Delete Application
        var delRes = await client.DeleteAsync($"/api/applications/{created.Id}");
        delRes.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify only duplicated item remains in active list
        var listRes = await client.GetAsync("/api/applications");
        var list = await listRes.Content.ReadFromJsonAsync<ApplicationListItemDto[]>(JsonOptions);
        list!.Should().HaveCount(1);
        list[0].Id.Should().Be(duplicated.Id);
    }
}

