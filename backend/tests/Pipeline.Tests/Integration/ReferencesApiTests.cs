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
using Pipeline.Application.Features.References.DTOs;
using Pipeline.Domain.Enums;
using Pipeline.Infrastructure.Persistence;
using Xunit;

namespace Pipeline.Tests.Integration;

public class ReferencesApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly WebApplicationFactory<Program> _factory;

    public ReferencesApiTests(WebApplicationFactory<Program> factory)
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
        var registerRequest = new RegisterRequest(email, "Password123!", "Test User");
        var res = await client.PostAsJsonAsync("/api/auth/register", registerRequest);
        res.StatusCode.Should().Be(HttpStatusCode.OK);

        var authRes = await res.Content.ReadFromJsonAsync<AuthResult>(JsonOptions);
        authRes.Should().NotBeNull();
        return authRes!.AccessToken;
    }

    [Fact]
    public async Task References_FullLifecycleAndConsentRule_WorksEndToEnd()
    {
        var dbName = Guid.NewGuid().ToString();
        var client = CreateAuthenticatedClient(dbName);
        var token = await RegisterAndGetTokenAsync(client, "refs.user@pipeline.local");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // 1. Create Reference with Consent = NotAsked
        var createReq = new CreateReferenceRequest(
            FullName: "Dave Bowman",
            Relationship: "Mission Commander",
            Email: "dave@discoveryone.space",
            Phone: "555-0101",
            Company: "Discovery One",
            Consent: ReferenceConsent.NotAsked,
            Notes: "Direct report on Jupiter project.");

        var createRes = await client.PostAsJsonAsync("/api/references", createReq);
        createRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var refRecord = await createRes.Content.ReadFromJsonAsync<ReferenceDetailDto>(JsonOptions);
        refRecord.Should().NotBeNull();
        refRecord!.FullName.Should().Be("Dave Bowman");

        // 2. Create an Application to share with
        var appReq = new CreateApplicationRequest(
            RoleTitle: "Astronaut Specialist",
            CompanyName: "HAL Corp",
            Status: ApplicationStatus.Interview);
        var appRes = await client.PostAsJsonAsync("/api/applications", appReq);
        appRes.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Created);
        var appRecord = await appRes.Content.ReadFromJsonAsync<ApplicationDetailDto>(JsonOptions);
        appRecord.Should().NotBeNull();

        // 3. Attempt to share without consent override -> Warning expected
        var shareNoOverride = new ShareReferenceRequest(appRecord!.Id, "Submitted CV", OverrideConsentWarning: false);
        var shareRes1 = await client.PostAsJsonAsync($"/api/references/{refRecord.Id}/share", shareNoOverride);
        shareRes1.StatusCode.Should().Be(HttpStatusCode.OK);
        var shareObj1 = await shareRes1.Content.ReadFromJsonAsync<ShareReferenceResponse>(JsonOptions);
        shareObj1.Should().NotBeNull();
        shareObj1!.Success.Should().BeFalse();
        shareObj1.WarningTriggered.Should().BeTrue();
        shareObj1.WarningMessage.Should().Contain("NotAsked");

        // 4. Share WITH consent override -> Succeeds
        var shareWithOverride = new ShareReferenceRequest(appRecord.Id, "Permission confirmed verbally", OverrideConsentWarning: true);
        var shareRes2 = await client.PostAsJsonAsync($"/api/references/{refRecord.Id}/share", shareWithOverride);
        shareRes2.StatusCode.Should().Be(HttpStatusCode.OK);
        var shareObj2 = await shareRes2.Content.ReadFromJsonAsync<ShareReferenceResponse>(JsonOptions);
        shareObj2.Should().NotBeNull();
        shareObj2!.Success.Should().BeTrue();
        shareObj2.SharedRecord.Should().NotBeNull();
        shareObj2.SharedRecord!.CompanyName.Should().Be("HAL Corp");

        // 5. Notify Reference
        var notifyRes = await client.PostAsync($"/api/references/{refRecord.Id}/notify", null);
        notifyRes.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // 6. Verify detail has shared apps and notification timestamp
        var detailRes = await client.GetAsync($"/api/references/{refRecord.Id}");
        detailRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var detail = await detailRes.Content.ReadFromJsonAsync<ReferenceDetailDto>(JsonOptions);
        detail.Should().NotBeNull();
        detail!.LastNotifiedAt.Should().NotBeNull();
        detail.SharedApplications.Should().HaveCount(1);
    }
}
