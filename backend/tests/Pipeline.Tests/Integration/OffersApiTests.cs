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
using Pipeline.Application.Features.Offers.DTOs;
using Pipeline.Domain.Enums;
using Pipeline.Infrastructure.Persistence;
using Xunit;

namespace Pipeline.Tests.Integration;

public class OffersApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly WebApplicationFactory<Program> _factory;

    public OffersApiTests(WebApplicationFactory<Program> factory)
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
    public async Task Offers_UpdateAndComparison_WorksEndToEnd()
    {
        var dbName = Guid.NewGuid().ToString();
        var client = CreateAuthenticatedClient(dbName);
        var token = await RegisterAndGetTokenAsync(client, "offers.user@pipeline.local");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // 1. Create two applications
        var appReq1 = new CreateApplicationRequest(
            RoleTitle: "Staff Software Engineer",
            CompanyName: "Google",
            Status: ApplicationStatus.Offer);
        var appRes1 = await client.PostAsJsonAsync("/api/applications", appReq1);
        var app1 = await appRes1.Content.ReadFromJsonAsync<ApplicationDetailDto>(JsonOptions);

        var appReq2 = new CreateApplicationRequest(
            RoleTitle: "Principal Architect",
            CompanyName: "Netflix",
            Status: ApplicationStatus.Offer);
        var appRes2 = await client.PostAsJsonAsync("/api/applications", appReq2);
        var app2 = await appRes2.Content.ReadFromJsonAsync<ApplicationDetailDto>(JsonOptions);

        // 2. Update offer details on app1
        var offerReq1 = new UpdateOfferDetailsRequest(
            OfferSalary: 220000m,
            OfferBonus: 40000m,
            OfferBenefits: "Full dental/vision, $10k annual travel stipend",
            OfferDeadline: DateTime.UtcNow.AddDays(7),
            OfferNegotiationNotes: "Negotiated additional sign-on bonus");
        var updateRes1 = await client.PutAsJsonAsync($"/api/offers/application/{app1!.Id}", offerReq1);
        updateRes1.StatusCode.Should().Be(HttpStatusCode.OK);

        // 3. Update offer details on app2
        var offerReq2 = new UpdateOfferDetailsRequest(
            OfferSalary: 260000m,
            OfferBonus: 50000m,
            OfferBenefits: "All cash comp model, top tier health coverage",
            OfferDeadline: DateTime.UtcNow.AddDays(10),
            OfferNegotiationNotes: "Asked for start date flexibility");
        var updateRes2 = await client.PutAsJsonAsync($"/api/offers/application/{app2!.Id}", offerReq2);
        updateRes2.StatusCode.Should().Be(HttpStatusCode.OK);

        // 4. Compare offers
        var compRes = await client.GetAsync($"/api/offers/compare?ids={app1.Id}&ids={app2.Id}");
        compRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var compView = await compRes.Content.ReadFromJsonAsync<OfferComparisonViewDto>(JsonOptions);

        compView.Should().NotBeNull();
        compView!.Offers.Should().HaveCount(2);
        compView.AvailableCriteria.Should().NotBeEmpty();
        compView.Offers.Should().Contain(o => o.TotalCompensation == 260000m);
        compView.Offers.Should().Contain(o => o.TotalCompensation == 310000m);
    }
}
