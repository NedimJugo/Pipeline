# Job Discovery — Extension Point Guide

Pipeline provides a clean, pluggable extension point for aggregating engineering opportunities from external feeds, ATS boards, scrapers, and APIs without requiring modifications to core application logic.

---

## 1. Architectural Overview

The Job Discovery subsystem consists of three core components:

1. **`IJobSourceConnector` Interface**: A standardized contract that fetches job listings from external sources.
2. **Hash Deduplication Engine**: Computes SHA-256 fingerprint (`CompanyName:Title:Location`) to prevent duplicate job entries across ingestion runs.
3. **Multi-Tenant State Management (`UserDiscoveredJobState`)**: Discovered jobs exist in a global opportunity pool, while individual user actions (`New`, `Saved`, `Dismissed`) remain completely isolated across accounts.

```
[ External RSS / API / Scraper ]
              │
              ▼
   [ IJobSourceConnector ]
              │
              ▼ (RawJob collection)
    [ Ingestion Service ]  ───► Compute SHA-256 Hash ───► Deduplicate
              │
              ▼
     [ DiscoveredJobs ] (Global Table)
              │
              ▼
  [ UserDiscoveredJobStates ] (Per-user: New / Saved / Dismissed)
              │
              ▼ (One-Click "Save to Wishlist")
   [ Tracked Applications ] (User-owned Wishlist Pipeline)
```

---

## 2. Connector Interface Definition

All custom connectors implement `IJobSourceConnector` located in `Pipeline.Application.Features.Discovery.Services`:

```csharp
namespace Pipeline.Application.Features.Discovery.Services;

public record RawJob(
    string ExternalId,
    string Title,
    string CompanyName,
    string? Location,
    string Url,
    string? Description,
    DateTimeOffset? PostedAt,
    string[] Tags
);

public interface IJobSourceConnector
{
    string SourceKey { get; }
    Task<IReadOnlyList<RawJob>> FetchAsync(JobSource source, CancellationToken ct);
}
```

---

## 3. Step-by-Step Tutorial: Implementing a Custom Connector

### Step 1: Create the Connector Class

Here is an example implementing an RSS connector for an engineering jobs feed:

```csharp
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.ServiceModel.Syndication;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Pipeline.Application.Features.Discovery.Services;
using Pipeline.Domain.Entities;

namespace Pipeline.Infrastructure.Services.Discovery;

public class CustomRssJobConnector : IJobSourceConnector
{
    private readonly HttpClient _httpClient;

    public CustomRssJobConnector(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    // Must match the sourceKey configured in the JobSource Config JSON
    public string SourceKey => "custom-rss-feed";

    public async Task<IReadOnlyList<RawJob>> FetchAsync(JobSource source, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(source.BaseUrl))
        {
            return Array.Empty<RawJob>();
        }

        using var response = await _httpClient.GetAsync(source.BaseUrl, ct);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var xmlReader = XmlReader.Create(stream);
        var feed = SyndicationFeed.Load(xmlReader);

        var results = new List<RawJob>();

        foreach (var item in feed.Items)
        {
            var title = item.Title?.Text ?? "Engineering Role";
            var url = item.Links.Count > 0 ? item.Links[0].Uri.ToString() : source.BaseUrl;
            var summary = item.Summary?.Text ?? "";

            // Parse company and role from feed conventions (e.g. "Stripe - Staff Engineer")
            var parts = title.Split(new[] { " - ", " at ", ":" }, StringSplitOptions.RemoveEmptyEntries);
            var company = parts.Length > 1 ? parts[0].Trim() : source.Name;
            var roleTitle = parts.Length > 1 ? parts[1].Trim() : title;

            results.Add(new RawJob(
                ExternalId: item.Id ?? url,
                Title: roleTitle,
                CompanyName: company,
                Location: "Remote",
                Url: url,
                Description: summary,
                PostedAt: item.PublishDate,
                Tags: new[] { "Engineering", "Remote" }
            ));
        }

        return results;
    }
}
```

### Step 2: Register in Dependency Injection

In `backend/src/Pipeline.Api/Program.cs`:

```csharp
builder.Services.AddHttpClient<CustomRssJobConnector>();
builder.Services.AddScoped<IJobSourceConnector, CustomRssJobConnector>();
```

### Step 3: Configure the Job Source in the Database

Insert a source row into `job_sources` table:

```sql
INSERT INTO job_sources (id, name, type, base_url, config, enabled, created_at)
VALUES (
    gen_random_uuid(),
    'Hacker News Engineering Feed',
    'Rss',
    'https://news.ycombinator.com/jobs.rss',
    '{"sourceKey": "custom-rss-feed"}',
    true,
    NOW()
);
```

---

## 4. REST API Endpoints

| Method | Endpoint | Description |
|---|---|---|
| `GET` | `/api/discovery/jobs?status=New&page=1` | List discovered jobs with user status filter |
| `GET` | `/api/discovery/sources` | List configured connector sources |
| `POST` | `/api/discovery/jobs/{id}/save` | Convert discovered job to a `Wishlist` application |
| `POST` | `/api/discovery/jobs/{id}/dismiss` | Mark discovered job as dismissed for current user |
| `POST` | `/api/discovery/ingest` | Trigger manual ingestion from enabled connectors |

---

## 5. Next-Action & Pipeline Conversion

When a user clicks **"Save to Wishlist"**:
1. Pipeline resolves or creates the `Company` record.
2. Creates an `Application` record with `Status = Wishlist`.
3. Links `DiscoveredJobId` to the source posting.
4. Updates user's status to `Saved`.
5. Emits an initial timeline entry so the application immediately integrates with pipeline metrics and conversion funnels.
