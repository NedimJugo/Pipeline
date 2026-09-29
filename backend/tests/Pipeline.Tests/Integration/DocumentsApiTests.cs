using System;
using System.Collections.Generic;
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
using Pipeline.Application.Features.Documents.DTOs;
using Pipeline.Domain.Enums;
using Pipeline.Infrastructure.Persistence;
using Xunit;

namespace Pipeline.Tests.Integration;

public class DocumentsApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly WebApplicationFactory<Program> _factory;

    public DocumentsApiTests(WebApplicationFactory<Program> factory)
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

    private static ByteArrayContent CreateFakePdfContent(string label)
    {
        var bytes = Encoding.UTF8.GetBytes($"%PDF-1.4 PDF Integration Test Content for {label}");
        var content = new ByteArrayContent(bytes);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        return content;
    }

    [Fact]
    public async Task DocumentsFlow_FullLifecycle_Upload_Versioning_Stats_And_Isolation()
    {
        var dbName = Guid.NewGuid().ToString();
        var clientA = CreateAuthenticatedClient(dbName);

        // 1. Register User A
        var regA = await clientA.PostAsJsonAsync("/api/auth/register", new RegisterRequest(
            "doc_user_a@pipeline.test", "Password123!", "User A"));
        regA.StatusCode.Should().Be(HttpStatusCode.OK);
        var authJsonA = await regA.Content.ReadFromJsonAsync<JsonElement>();
        var tokenA = authJsonA.GetProperty("accessToken").GetString();
        clientA.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);

        // 2. Upload Document with file (v1)
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent("Frontend Engineer CV"), "title");
        form.Add(new StringContent("CV"), "type");
        form.Add(new StringContent("Tailored for modern web roles"), "description");
        form.Add(new StringContent("v1"), "versionLabel");
        form.Add(CreateFakePdfContent("v1"), "file", "cv_v1.pdf");

        var uploadResponse = await clientA.PostAsync("/api/documents/with-file", form);
        uploadResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var doc = await uploadResponse.Content.ReadFromJsonAsync<DocumentDto>(JsonOptions);
        doc.Should().NotBeNull();
        doc!.Title.Should().Be("Frontend Engineer CV");
        doc.Versions.Should().HaveCount(1);
        var v1 = doc.Versions.First();
        v1.VersionLabel.Should().Be("v1");
        v1.IsDefault.Should().BeTrue();

        // 3. Upload Version 2 (as default)
        using var formV2 = new MultipartFormDataContent();
        formV2.Add(new StringContent("v2"), "versionLabel");
        formV2.Add(new StringContent("Added GraphQL and Next.js"), "notes");
        formV2.Add(new StringContent("true"), "isDefault");
        formV2.Add(CreateFakePdfContent("v2"), "file", "cv_v2.pdf");

        var v2Response = await clientA.PostAsync($"/api/documents/{doc.Id}/versions", formV2);
        v2Response.StatusCode.Should().Be(HttpStatusCode.OK);
        var v2 = await v2Response.Content.ReadFromJsonAsync<DocumentVersionDto>(JsonOptions);
        v2.Should().NotBeNull();
        v2!.VersionLabel.Should().Be("v2");
        v2.IsDefault.Should().BeTrue();

        // 4. Verify Document details has v2 as default
        var getDoc = await clientA.GetFromJsonAsync<DocumentDto>($"/api/documents/{doc.Id}", JsonOptions);
        getDoc!.Versions.Should().HaveCount(2);
        getDoc.DefaultVersionId.Should().Be(v2.Id);

        // 5. Test Download URL endpoint
        var dlResponse = await clientA.GetAsync($"/api/document-versions/{v2.Id}/download");
        dlResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var dl = await dlResponse.Content.ReadFromJsonAsync<DocumentDownloadDto>(JsonOptions);
        dl.Should().NotBeNull();
        dl!.FileName.Should().Be("cv_v2.pdf");
        dl.Url.Should().NotBeNullOrWhiteSpace();

        // 6. Test Stream endpoint
        var streamResponse = await clientA.GetAsync($"/api/document-versions/{v2.Id}/stream");
        streamResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        streamResponse.Content.Headers.ContentType?.MediaType.Should().Be("application/pdf");
        var bytes = await streamResponse.Content.ReadAsByteArrayAsync();
        bytes.Length.Should().BeGreaterThan(10);

        // 7. Create Application and link v2
        var appResponse = await clientA.PostAsJsonAsync("/api/applications", new CreateApplicationRequest(
            RoleTitle: "Lead Frontend Engineer",
            CompanyName: "Vercel",
            Status: ApplicationStatus.Interview,
            DocumentVersionCvId: v2.Id));
        appResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // 8. Check Document stats
        var statsResponse = await clientA.GetAsync("/api/documents/stats");
        statsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var stats = await statsResponse.Content.ReadFromJsonAsync<DocumentStatsSummaryDto>(JsonOptions);
        stats.Should().NotBeNull();
        stats!.CvVersionStats.Should().Contain(s => s.VersionId == v2.Id && s.SentCount == 1 && s.InterviewCount == 1);

        // 9. Multi-tenant isolation: User B cannot access User A's document
        var clientB = CreateAuthenticatedClient(dbName);
        var regB = await clientB.PostAsJsonAsync("/api/auth/register", new RegisterRequest(
            "doc_user_b@pipeline.test", "Password123!", "User B"));
        var authJsonB = await regB.Content.ReadFromJsonAsync<JsonElement>();
        var tokenB = authJsonB.GetProperty("accessToken").GetString();
        clientB.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenB);

        var getDocB = await clientB.GetAsync($"/api/documents/{doc.Id}");
        getDocB.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var getVersionB = await clientB.GetAsync($"/api/document-versions/{v2.Id}/download");
        getVersionB.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
