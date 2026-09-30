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
using Pipeline.Application.Features.Analytics.DTOs;
using Pipeline.Application.Features.Applications.DTOs;
using Pipeline.Application.Features.Auth.DTOs;
using Pipeline.Domain.Enums;
using Pipeline.Infrastructure.Persistence;
using Xunit;

namespace Pipeline.Tests.Integration;

public class AnalyticsApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly WebApplicationFactory<Program> _factory;

    public AnalyticsApiTests(WebApplicationFactory<Program> factory)
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
        var registerRequest = new RegisterRequest(email, "Password123!", "Analytics Test User");
        var res = await client.PostAsJsonAsync("/api/auth/register", registerRequest, JsonOptions);
        res.StatusCode.Should().Be(HttpStatusCode.OK);

        var authRes = await res.Content.ReadFromJsonAsync<AuthResult>(JsonOptions);
        authRes.Should().NotBeNull();
        return authRes!.AccessToken;
    }

    [Fact]
    public async Task AnalyticsApi_ReturnsOverviewAndEnforcesMultiTenantIsolation()
    {
        var dbName = Guid.NewGuid().ToString();
        var clientUser1 = CreateAuthenticatedClient(dbName);
        var clientUser2 = CreateAuthenticatedClient(dbName);

        var token1 = await RegisterAndGetTokenAsync(clientUser1, "user1.analytics@example.com");
        var token2 = await RegisterAndGetTokenAsync(clientUser2, "user2.analytics@example.com");

        clientUser1.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token1);
        clientUser2.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token2);

        // User 1 creates 3 applications
        var app1Res = await clientUser1.PostAsJsonAsync("/api/applications", new CreateApplicationRequest(
            CompanyName: "Google",
            RoleTitle: "Staff Software Engineer",
            Source: ApplicationSource.Referral,
            WorkMode: WorkMode.Remote,
            EmploymentType: EmploymentType.FullTime,
            Status: ApplicationStatus.Interview
        ), JsonOptions);
        app1Res.EnsureSuccessStatusCode();

        var app2Res = await clientUser1.PostAsJsonAsync("/api/applications", new CreateApplicationRequest(
            CompanyName: "Amazon",
            RoleTitle: "Principal Systems Architect",
            Source: ApplicationSource.LinkedIn,
            WorkMode: WorkMode.Hybrid,
            EmploymentType: EmploymentType.FullTime,
            Status: ApplicationStatus.Screening
        ), JsonOptions);
        app2Res.EnsureSuccessStatusCode();

        var app3Res = await clientUser1.PostAsJsonAsync("/api/applications", new CreateApplicationRequest(
            CompanyName: "Microsoft",
            RoleTitle: "Cloud Engineer",
            Source: ApplicationSource.LinkedIn,
            WorkMode: WorkMode.Remote,
            EmploymentType: EmploymentType.FullTime,
            Status: ApplicationStatus.Offer
        ), JsonOptions);
        app3Res.EnsureSuccessStatusCode();

        // 1. Fetch User 1 Analytics Overview
        var overviewRes1 = await clientUser1.GetAsync("/api/analytics/overview");
        overviewRes1.StatusCode.Should().Be(HttpStatusCode.OK);

        var overview1 = await overviewRes1.Content.ReadFromJsonAsync<AnalyticsOverviewDto>(JsonOptions);
        overview1.Should().NotBeNull();
        overview1!.TotalApplications.Should().Be(3);
        overview1.Funnel.Should().NotBeEmpty();

        var appliedFunnel = overview1.Funnel.FirstOrDefault(f => f.Stage == "Applied");
        appliedFunnel.Should().NotBeNull();
        appliedFunnel!.Count.Should().Be(3);

        var interviewFunnel = overview1.Funnel.FirstOrDefault(f => f.Stage == "Interview");
        interviewFunnel.Should().NotBeNull();
        // Screening is not Interview, but Interview & Offer are >= Interview (2)
        interviewFunnel!.Count.Should().Be(2);

        // Check BySource
        overview1.BySource.Should().HaveCount(2);
        var referralSource = overview1.BySource.FirstOrDefault(s => s.Label == "Referral");
        referralSource.Should().NotBeNull();
        referralSource!.TotalApplications.Should().Be(1);

        // 2. Fetch User 2 Analytics Overview (Multi-tenant check)
        var overviewRes2 = await clientUser2.GetAsync("/api/analytics/overview");
        overviewRes2.StatusCode.Should().Be(HttpStatusCode.OK);

        var overview2 = await overviewRes2.Content.ReadFromJsonAsync<AnalyticsOverviewDto>(JsonOptions);
        overview2.Should().NotBeNull();
        overview2!.TotalApplications.Should().Be(0);
        overview2.Funnel.All(f => f.Count == 0).Should().BeTrue();
    }
}
