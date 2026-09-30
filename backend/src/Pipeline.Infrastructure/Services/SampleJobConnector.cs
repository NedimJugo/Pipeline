using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Pipeline.Application.Features.Discovery.Services;
using Pipeline.Domain.Entities;

namespace Pipeline.Infrastructure.Services;

public class SampleJobConnector : IJobSourceConnector
{
    public string SourceKey => "sample-tech-feed";

    public Task<IReadOnlyList<RawJob>> FetchAsync(JobSource source, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var jobs = new List<RawJob>
        {
            new RawJob(
                ExternalId: "gh-8901",
                Title: "Senior Distributed Systems Engineer",
                CompanyName: "GitHub",
                Location: "Remote (US/EU)",
                Url: "https://github.com/about/careers/distributed-systems",
                Description: "Join the Core Platform team building next-generation Git storage and high-throughput background processing infrastructure supporting millions of developers.",
                PostedAt: now.AddHours(-14),
                Tags: new[] { "Go", "C#", "Kubernetes", "Distributed Systems", "Remote" }
            ),
            new RawJob(
                ExternalId: "vercel-402",
                Title: "Staff Edge Infrastructure Engineer",
                CompanyName: "Vercel",
                Location: "Remote (Global)",
                Url: "https://vercel.com/careers/edge-infra",
                Description: "Architect our global edge routing networks and serverless compute primitives. Optimize p99 cold starts and multi-region data caching.",
                PostedAt: now.AddDays(-1),
                Tags: new[] { "Rust", "Node.js", "Edge Networks", "DNS", "CDN" }
            ),
            new RawJob(
                ExternalId: "stripe-119",
                Title: "Lead Reliability & Infrastructure Engineer",
                CompanyName: "Stripe",
                Location: "San Francisco, CA (Hybrid)",
                Url: "https://stripe.com/jobs/infrastructure-lead",
                Description: "Ensure five-nines availability across Stripe's core payment gateways and global transaction settlement pipelines.",
                PostedAt: now.AddDays(-2),
                Tags: new[] { "Java", "Go", "Observability", "PostgreSQL", "Kafka" }
            ),
            new RawJob(
                ExternalId: "linear-301",
                Title: "Full-Stack Product Engineer",
                CompanyName: "Linear",
                Location: "Remote (Europe)",
                Url: "https://linear.app/careers/product-engineer",
                Description: "Craft high-performance, keyboard-first issue tracking and project planning experiences. Work with React, TypeScript, and local-first SQLite sync.",
                PostedAt: now.AddDays(-3),
                Tags: new[] { "TypeScript", "React", "GraphQL", "Local-first", "UI/UX" }
            ),
            new RawJob(
                ExternalId: "figma-550",
                Title: "Principal Systems Architect",
                CompanyName: "Figma",
                Location: "New York, NY (Hybrid)",
                Url: "https://figma.com/careers/systems-architect",
                Description: "Drive the architectural evolution of our multi-player collaboration engine and real-time graphics rendering canvas in WebAssembly and C++.",
                PostedAt: now.AddDays(-4),
                Tags: new[] { "C++", "WebAssembly", "Real-Time Sync", "Canvas", "Algorithms" }
            ),
            new RawJob(
                ExternalId: "datadog-884",
                Title: "Senior Backend Engineer - APM Tracing",
                CompanyName: "Datadog",
                Location: "Remote (US)",
                Url: "https://datadoghq.com/careers/apm-backend",
                Description: "Scale our distributed tracing backends processing billions of spans per second with low latency and high compression.",
                PostedAt: now.AddDays(-5),
                Tags: new[] { "Go", "Kafka", "Distributed Tracing", "OpenTelemetry" }
            )
        };

        return Task.FromResult<IReadOnlyList<RawJob>>(jobs);
    }
}
