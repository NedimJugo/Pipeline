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
using Pipeline.Application.Features.Auth.DTOs;
using Pipeline.Application.Features.Settings.DTOs;
using Pipeline.Infrastructure.Persistence;
using Xunit;

namespace Pipeline.Tests.Integration;

public class IntegrationsApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly WebApplicationFactory<Program> _factory;

    public IntegrationsApiTests(WebApplicationFactory<Program> factory)
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
        var response = await client.PostAsJsonAsync("/api/auth/register", registerRequest);
        response.EnsureSuccessStatusCode();

        var authResponse = await response.Content.ReadFromJsonAsync<AuthResult>(JsonOptions);
        return authResponse!.AccessToken;
    }

    [Fact]
    public async Task GetIntegrations_WithoutAuth_ReturnsUnauthorized()
    {
        var client = CreateAuthenticatedClient(Guid.NewGuid().ToString());
        var response = await client.GetAsync("/api/settings/integrations");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAndPutIntegrations_WorksCorrectly()
    {
        var client = CreateAuthenticatedClient(Guid.NewGuid().ToString());
        var token = await RegisterAndGetTokenAsync(client, $"user_{Guid.NewGuid():N}@test.local");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // 1. Initial GET
        var getRes = await client.GetAsync("/api/settings/integrations");
        getRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var initial = await getRes.Content.ReadFromJsonAsync<IntegrationSettingsDto>(JsonOptions);
        initial.Should().NotBeNull();
        initial!.IsEnvFallbackSmtp.Should().BeTrue();

        // 2. PUT update
        var updateReq = new UpdateIntegrationSettingsRequest(
            UseCustomSmtp: true,
            SmtpHost: "smtp.mailgun.org",
            SmtpPort: 587,
            SmtpUser: "postmaster@mailgun.org",
            SmtpPassword: "supersecretpass",
            SmtpFrom: "notifications@example.com",
            UseCustomGoogle: true,
            GoogleClientId: "test-client-id",
            GoogleClientSecret: "test-client-secret",
            StorageProvider: "S3"
        );

        var putRes = await client.PutAsJsonAsync("/api/settings/integrations", updateReq);
        putRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var updated = await putRes.Content.ReadFromJsonAsync<IntegrationSettingsDto>(JsonOptions);
        updated.Should().NotBeNull();
        updated!.UseCustomSmtp.Should().BeTrue();
        updated.IsEnvFallbackSmtp.Should().BeFalse();
        updated.SmtpHost.Should().Be("smtp.mailgun.org");
        updated.SmtpPort.Should().Be(587);
        updated.HasSmtpPassword.Should().BeTrue();
        updated.StorageProvider.Should().Be("S3");

        // 3. Test email endpoint call
        var testEmailReq = new TestEmailRequest("test@recipient.com");
        var testRes = await client.PostAsJsonAsync("/api/settings/integrations/test-email", testEmailReq);
        testRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var testResult = await testRes.Content.ReadFromJsonAsync<TestEmailResultDto>(JsonOptions);
        testResult.Should().NotBeNull();
    }
}
