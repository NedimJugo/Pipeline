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
using Pipeline.Application.Features.Contacts.DTOs;
using Pipeline.Application.Features.Dashboard.DTOs;
using Pipeline.Application.Features.Tasks.DTOs;
using Pipeline.Application.Features.Templates.DTOs;
using Pipeline.Domain.Enums;
using Pipeline.Infrastructure.Persistence;
using Xunit;

namespace Pipeline.Tests.Integration;

public class TasksApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly WebApplicationFactory<Program> _factory;

    public TasksApiTests(WebApplicationFactory<Program> factory)
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
    public async Task TasksAndTemplates_CompleteIntegration_With_Isolation()
    {
        var dbName = Guid.NewGuid().ToString();
        var clientA = CreateAuthenticatedClient(dbName);
        var clientB = CreateAuthenticatedClient(dbName);

        // 1. Register User A
        var regA = await clientA.PostAsJsonAsync("/api/auth/register", new RegisterRequest(
            "user_a_tasks@pipeline.test", "Password123!", "Alice Wonder"));
        regA.StatusCode.Should().Be(HttpStatusCode.OK);
        var authJsonA = await regA.Content.ReadFromJsonAsync<JsonElement>();
        var tokenA = authJsonA.GetProperty("accessToken").GetString();
        clientA.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);

        // 2. Register User B
        var regB = await clientB.PostAsJsonAsync("/api/auth/register", new RegisterRequest(
            "user_b_tasks@pipeline.test", "Password123!", "Bob Builder"));
        regB.StatusCode.Should().Be(HttpStatusCode.OK);
        var authJsonB = await regB.Content.ReadFromJsonAsync<JsonElement>();
        var tokenB = authJsonB.GetProperty("accessToken").GetString();
        clientB.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenB);

        // 3. User A creates a Task
        var taskCreateReq = new CreateTaskRequest(
            Title: "Send portfolio link",
            Notes: "Add link to personal projects",
            DueAt: DateTime.UtcNow.AddDays(1));

        var taskCreateRes = await clientA.PostAsJsonAsync("/api/tasks", taskCreateReq);
        taskCreateRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdTask = await taskCreateRes.Content.ReadFromJsonAsync<TaskItemDto>(JsonOptions);
        createdTask.Should().NotBeNull();
        createdTask!.Title.Should().Be("Send portfolio link");

        // 4. User A completes and then snoozes the task
        var completeRes = await clientA.PostAsJsonAsync($"/api/tasks/{createdTask.Id}/complete", new { isCompleted = true });
        completeRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var completedTask = await completeRes.Content.ReadFromJsonAsync<TaskItemDto>(JsonOptions);
        completedTask!.CompletedAt.Should().NotBeNull();

        var snoozeRes = await clientA.PostAsJsonAsync($"/api/tasks/{createdTask.Id}/snooze", new SnoozeTaskRequest(3));
        snoozeRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var snoozedTask = await snoozeRes.Content.ReadFromJsonAsync<TaskItemDto>(JsonOptions);
        snoozedTask!.DueAt.Should().NotBeNull();
        snoozedTask.DueAt!.Value.Date.Should().Be(DateTime.UtcNow.AddDays(3).Date);

        // 5. User B CANNOT see User A's task (isolation)
        var userBGetTaskRes = await clientB.GetAsync($"/api/tasks/{createdTask.Id}");
        userBGetTaskRes.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var userBTasksList = await clientB.GetFromJsonAsync<PagedTasksResult>("/api/tasks", JsonOptions);
        userBTasksList!.Items.Should().NotContain(t => t.Id == createdTask.Id);

        // 6. User A gets email templates (seeded system templates)
        var templatesRes = await clientA.GetFromJsonAsync<List<EmailTemplateDto>>("/api/templates", JsonOptions);
        templatesRes.Should().NotBeNull();
        templatesRes!.Should().HaveCount(6);

        // 7. Create application & contact for User A to test template rendering
        var appRes = await clientA.PostAsJsonAsync("/api/applications", new CreateApplicationRequest(
            RoleTitle: "Lead Architect",
            CompanyName: "Skyline Cloud",
            Status: ApplicationStatus.Interview));
        var app = await appRes.Content.ReadFromJsonAsync<ApplicationListItemDto>(JsonOptions);

        var contactRes = await clientA.PostAsJsonAsync("/api/contacts", new CreateContactRequest(
            FullName: "Sarah Connor",
            Role: "Head of Talent",
            CompanyId: app!.CompanyId));
        var contact = await contactRes.Content.ReadFromJsonAsync<ContactDetailDto>(JsonOptions);

        var followUpTemplate = templatesRes.First(t => t.Category == EmailTemplateCategory.FollowUp);
        var renderRes = await clientA.PostAsJsonAsync($"/api/templates/{followUpTemplate.Id}/render", new RenderEmailTemplateRequest(
            ApplicationId: app.Id,
            ContactId: contact!.Id));
        renderRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var rendered = await renderRes.Content.ReadFromJsonAsync<RenderedEmailTemplateDto>(JsonOptions);
        rendered.Should().NotBeNull();
        rendered!.Subject.Should().Contain("Lead Architect").And.Contain("Skyline Cloud");
        rendered.Body.Should().Contain("Sarah Connor").And.Contain("Lead Architect").And.Contain("Alice Wonder");

        // 8. Test Dashboard endpoint
        var dashRes = await clientA.GetAsync("/api/dashboard");
        dashRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var dashboard = await dashRes.Content.ReadFromJsonAsync<DashboardSummaryDto>(JsonOptions);
        dashboard.Should().NotBeNull();
        dashboard!.Greeting.Should().Contain("Alice Wonder");
        dashboard.SearchStatus.Should().Be(SearchStatus.Active);

        // 9. Test Automation evaluation endpoint
        var autoRes = await clientA.PostAsync("/api/automation/evaluate", null);
        autoRes.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
