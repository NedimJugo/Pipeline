using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
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
using Pipeline.Application.Features.Users.DTOs;
using Pipeline.Domain.Enums;
using Pipeline.Infrastructure.Persistence;
using Xunit;

namespace Pipeline.Tests.Integration;

public class MeApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly WebApplicationFactory<Program> _factory;

    public MeApiTests(WebApplicationFactory<Program> factory)
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
        var registerRequest = new RegisterRequest(email, "Password123!", "Settings Test User");
        var res = await client.PostAsJsonAsync("/api/auth/register", registerRequest, JsonOptions);
        res.StatusCode.Should().Be(HttpStatusCode.OK);

        var authRes = await res.Content.ReadFromJsonAsync<AuthResult>(JsonOptions);
        authRes.Should().NotBeNull();
        return authRes!.AccessToken;
    }

    [Fact]
    public async Task MeApi_GetAndPutProfile_WorksAndEnforcesIsolation()
    {
        var dbName = Guid.NewGuid().ToString();
        var clientUser1 = CreateAuthenticatedClient(dbName);
        var clientUser2 = CreateAuthenticatedClient(dbName);

        var token1 = await RegisterAndGetTokenAsync(clientUser1, "user1.settings@example.com");
        var token2 = await RegisterAndGetTokenAsync(clientUser2, "user2.settings@example.com");

        clientUser1.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token1);
        clientUser2.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token2);

        // 1. Get initial profile for User 1
        var profileRes1 = await clientUser1.GetAsync("/api/me");
        profileRes1.StatusCode.Should().Be(HttpStatusCode.OK);
        var profile1 = await profileRes1.Content.ReadFromJsonAsync<UserSettingsProfileDto>(JsonOptions);
        profile1!.Email.Should().Be("user1.settings@example.com");

        // 2. Update profile for User 1
        var updateReq = new UpdateProfileRequest(
            DisplayName: "Alex User One",
            TargetRole: "Staff Platform Engineer",
            Seniority: "Staff",
            Location: "San Francisco, CA",
            SalaryExpectationMin: 190000,
            SalaryExpectationMax: 230000,
            Currency: "USD",
            SearchStatus: SearchStatus.Active,
            Timezone: "America/Los_Angeles"
        );
        var updateRes = await clientUser1.PutAsJsonAsync("/api/me", updateReq, JsonOptions);
        updateRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var updatedProfile = await updateRes.Content.ReadFromJsonAsync<UserSettingsProfileDto>(JsonOptions);
        updatedProfile!.DisplayName.Should().Be("Alex User One");
        updatedProfile.TargetRole.Should().Be("Staff Platform Engineer");
        updatedProfile.SalaryExpectationMin.Should().Be(190000);

        // 3. User 2 profile is untouched (isolation)
        var profileRes2 = await clientUser2.GetAsync("/api/me");
        var profile2 = await profileRes2.Content.ReadFromJsonAsync<UserSettingsProfileDto>(JsonOptions);
        profile2!.Email.Should().Be("user2.settings@example.com");
        profile2.DisplayName.Should().NotBe("Alex User One");

        // 4. Update preferences
        var prefRes = await clientUser1.PutAsJsonAsync("/api/me/preferences", new UpdatePreferencesRequest(StaleAfterDays: 21), JsonOptions);
        prefRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var prefProfile = await prefRes.Content.ReadFromJsonAsync<UserSettingsProfileDto>(JsonOptions);
        prefProfile!.StaleAfterDays.Should().Be(21);
    }

    [Fact]
    public async Task MeApi_ExportGdprAndSeedDemo_WorksSuccessfully()
    {
        var dbName = Guid.NewGuid().ToString();
        var client = CreateAuthenticatedClient(dbName);
        var token = await RegisterAndGetTokenAsync(client, "user.gdpr@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // 1. Seed demo data
        var seedRes = await client.PostAsync("/api/me/seed-demo", null);
        seedRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // 2. Export GDPR JSON archive
        var exportRes = await client.GetAsync("/api/me/export");
        exportRes.StatusCode.Should().Be(HttpStatusCode.OK);
        exportRes.Content.Headers.ContentType!.MediaType.Should().Be("application/json");

        var jsonStr = await exportRes.Content.ReadAsStringAsync();
        jsonStr.Should().Contain("Settings Test User");
        jsonStr.Should().Contain("Stripe");
        jsonStr.Should().Contain("Staff Backend Engineer");
    }

    [Fact]
    public async Task ApplicationsApi_ExportAndImportCsv_WorksEndToEnd()
    {
        var dbName = Guid.NewGuid().ToString();
        var client = CreateAuthenticatedClient(dbName);
        var token = await RegisterAndGetTokenAsync(client, "user.csv@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // 1. Create an application
        await client.PostAsJsonAsync("/api/applications", new CreateApplicationRequest(
            CompanyName: "Stripe",
            RoleTitle: "Backend Lead",
            Source: ApplicationSource.Referral,
            WorkMode: WorkMode.Remote,
            Status: ApplicationStatus.Interview,
            SalaryMin: 170000,
            SalaryMax: 200000
        ), JsonOptions);

        // 2. Export CSV
        var exportRes = await client.GetAsync("/api/applications/export");
        exportRes.StatusCode.Should().Be(HttpStatusCode.OK);
        exportRes.Content.Headers.ContentType!.MediaType.Should().Be("text/csv");

        var csvText = await exportRes.Content.ReadAsStringAsync();
        csvText.Should().Contain("CompanyName,RoleTitle,Status");
        csvText.Should().Contain("Stripe");
        csvText.Should().Contain("Backend Lead");

        // 3. Import new CSV
        var importCsv = "CompanyName,RoleTitle,Status,Source,WorkMode,SalaryMin\n" +
                        "Airbnb,Platform Architect,Screening,LinkedIn,Remote,185000\n";

        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(importCsv));
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("text/csv");
        content.Add(fileContent, "file", "import.csv");

        var importRes = await client.PostAsync("/api/applications/import", content);
        importRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var importResult = await importRes.Content.ReadFromJsonAsync<CsvImportResultDto>(JsonOptions);
        importResult.Should().NotBeNull();
        importResult!.CreatedCount.Should().Be(1);

        // Verify imported application is in list
        var appsRes = await client.GetAsync("/api/applications");
        var apps = await appsRes.Content.ReadFromJsonAsync<ApplicationListItemDto[]>(JsonOptions);
        apps.Should().Contain(a => a.RoleTitle == "Platform Architect" && a.CompanyName == "Airbnb");
    }

    [Fact]
    public async Task MeApi_DeleteAccount_PurgesUserAndData()
    {
        var dbName = Guid.NewGuid().ToString();
        var client = CreateAuthenticatedClient(dbName);
        var token = await RegisterAndGetTokenAsync(client, "user.delete@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Create an application first
        await client.PostAsJsonAsync("/api/applications", new CreateApplicationRequest(
            CompanyName: "OpenAI",
            RoleTitle: "Research Engineer",
            Status: ApplicationStatus.Wishlist
        ), JsonOptions);

        // Delete account
        var delRes = await client.DeleteAsync("/api/me");
        delRes.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Subsequent GET /api/me should fail with 401 or 404
        var getRes = await client.GetAsync("/api/me");
        getRes.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.NotFound, HttpStatusCode.InternalServerError);
    }
}
