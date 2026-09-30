using System;
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
using Pipeline.Application.Features.Discovery.DTOs;
using Pipeline.Domain.Enums;
using Pipeline.Infrastructure.Persistence;
using Xunit;

namespace Pipeline.Tests.Integration;

public class DiscoveryApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly WebApplicationFactory<Program> _factory;

    public DiscoveryApiTests(WebApplicationFactory<Program> factory)
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

    private async Task<string> RegisterAndGetTokenAsync(HttpClient client, string email)
    {
        var registerRequest = new RegisterRequest(email, "Password123!", "Discovery Test User");
        var res = await client.PostAsJsonAsync("/api/auth/register", registerRequest, JsonOptions);
        res.StatusCode.Should().Be(HttpStatusCode.OK);

        var authRes = await res.Content.ReadFromJsonAsync<AuthResult>(JsonOptions);
        authRes.Should().NotBeNull();
        return authRes!.AccessToken;
    }

    [Fact]
    public async Task GetDiscoveredJobs_ReturnsAutoIngestedJobs()
    {
        var dbName = Guid.NewGuid().ToString();
        var client = CreateAuthenticatedClient(dbName);
        var token = await RegisterAndGetTokenAsync(client, "discovery1@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var res = await client.GetAsync("/api/discovery/jobs");

        // Assert
        res.StatusCode.Should().Be(HttpStatusCode.OK);
        var paged = await res.Content.ReadFromJsonAsync<DiscoveredJobListDto>(JsonOptions);
        paged.Should().NotBeNull();
        paged!.TotalCount.Should().BeGreaterThan(0);
        paged.Items.Should().NotBeEmpty();
        paged.Items[0].Status.Should().Be("New");
    }

    [Fact]
    public async Task SaveJobToWishlist_CreatesApplicationAndSetsSavedState()
    {
        var dbName = Guid.NewGuid().ToString();
        var client = CreateAuthenticatedClient(dbName);
        var token = await RegisterAndGetTokenAsync(client, "discovery2@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // 1. Get first job
        var jobsRes = await client.GetAsync("/api/discovery/jobs");
        var paged = await jobsRes.Content.ReadFromJsonAsync<DiscoveredJobListDto>(JsonOptions);
        var targetJob = paged!.Items.First();

        // 2. Save job to wishlist
        var saveRes = await client.PostAsync($"/api/discovery/jobs/{targetJob.Id}/save", null);
        saveRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var createdApp = await saveRes.Content.ReadFromJsonAsync<ApplicationDetailDto>(JsonOptions);
        createdApp.Should().NotBeNull();
        createdApp!.RoleTitle.Should().Be(targetJob.Title);
        createdApp.Status.Should().Be(ApplicationStatus.Wishlist);

        // 3. Verify in applications list
        var appsRes = await client.GetAsync($"/api/applications/{createdApp.Id}");
        appsRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // 4. Verify in saved discovery jobs
        var savedRes = await client.GetAsync("/api/discovery/jobs?status=Saved");
        var savedPaged = await savedRes.Content.ReadFromJsonAsync<DiscoveredJobListDto>(JsonOptions);
        savedPaged!.Items.Should().Contain(j => j.Id == targetJob.Id);
    }

    [Fact]
    public async Task DismissJob_MarksJobAsDismissed()
    {
        var dbName = Guid.NewGuid().ToString();
        var client = CreateAuthenticatedClient(dbName);
        var token = await RegisterAndGetTokenAsync(client, "discovery3@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // 1. Get first job
        var jobsRes = await client.GetAsync("/api/discovery/jobs");
        var paged = await jobsRes.Content.ReadFromJsonAsync<DiscoveredJobListDto>(JsonOptions);
        var targetJob = paged!.Items.First();

        // 2. Dismiss
        var dismissRes = await client.PostAsync($"/api/discovery/jobs/{targetJob.Id}/dismiss", null);
        dismissRes.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // 3. Verify in dismissed discovery jobs
        var dismissedRes = await client.GetAsync("/api/discovery/jobs?status=Dismissed");
        var dismissedPaged = await dismissedRes.Content.ReadFromJsonAsync<DiscoveredJobListDto>(JsonOptions);
        dismissedPaged!.Items.Should().Contain(j => j.Id == targetJob.Id);
    }

    [Fact]
    public async Task UserIsolation_UserACannotAffectUserBDiscoveryState()
    {
        var dbName = Guid.NewGuid().ToString();
        var clientUser1 = CreateAuthenticatedClient(dbName);
        var clientUser2 = CreateAuthenticatedClient(dbName);

        var token1 = await RegisterAndGetTokenAsync(clientUser1, "user1.discovery@example.com");
        var token2 = await RegisterAndGetTokenAsync(clientUser2, "user2.discovery@example.com");

        clientUser1.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token1);
        clientUser2.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token2);

        // User 1 gets jobs and saves the first one
        var jobsRes = await clientUser1.GetAsync("/api/discovery/jobs");
        var paged1 = await jobsRes.Content.ReadFromJsonAsync<DiscoveredJobListDto>(JsonOptions);
        var targetJob = paged1!.Items.First();

        var saveRes = await clientUser1.PostAsync($"/api/discovery/jobs/{targetJob.Id}/save", null);
        saveRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var createdApp = await saveRes.Content.ReadFromJsonAsync<ApplicationDetailDto>(JsonOptions);

        // User 2 inspects discovery jobs
        var user2JobsRes = await clientUser2.GetAsync("/api/discovery/jobs");
        var paged2 = await user2JobsRes.Content.ReadFromJsonAsync<DiscoveredJobListDto>(JsonOptions);
        var user2Job = paged2!.Items.First(j => j.Id == targetJob.Id);

        // User 2 should see this job as "New", NOT "Saved"
        user2Job.Status.Should().Be("New");

        // User 2 cannot view User 1's created application
        var user2AppRes = await clientUser2.GetAsync($"/api/applications/{createdApp!.Id}");
        user2AppRes.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
