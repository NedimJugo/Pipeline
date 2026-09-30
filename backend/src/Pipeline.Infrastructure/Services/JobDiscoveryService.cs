using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Pipeline.Application.Common.Interfaces;
using Pipeline.Application.Features.Applications.DTOs;
using Pipeline.Application.Features.Applications.Services;
using Pipeline.Application.Features.Discovery.DTOs;
using Pipeline.Application.Features.Discovery.Services;
using Pipeline.Domain.Entities;
using Pipeline.Domain.Enums;
using Pipeline.Infrastructure.Persistence;

namespace Pipeline.Infrastructure.Services;

public class JobDiscoveryService : IJobDiscoveryService
{
    private readonly PipelineDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IApplicationService _applicationService;
    private readonly IEnumerable<IJobSourceConnector> _connectors;

    public JobDiscoveryService(
        PipelineDbContext dbContext,
        ICurrentUserService currentUserService,
        IApplicationService applicationService,
        IEnumerable<IJobSourceConnector> connectors)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _applicationService = applicationService;
        _connectors = connectors;
    }

    public async Task<DiscoveredJobListDto> GetDiscoveredJobsAsync(
        int page = 1,
        int pageSize = 20,
        string? status = null,
        string? search = null,
        Guid? sourceId = null,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        // Auto-seed sample jobs if table is empty
        if (!await _dbContext.DiscoveredJobs.AnyAsync(ct))
        {
            await IngestJobsAsync(ct);
        }

        var userId = _currentUserService.UserId;
        var userStates = userId.HasValue
            ? await _dbContext.UserDiscoveredJobStates
                .Where(s => s.UserId == userId.Value)
                .ToDictionaryAsync(s => s.DiscoveredJobId, s => s.State, ct)
            : new Dictionary<Guid, string>();

        var query = _dbContext.DiscoveredJobs
            .Include(j => j.Source)
            .AsNoTracking();

        if (sourceId.HasValue && sourceId.Value != Guid.Empty)
        {
            query = query.Where(j => j.SourceId == sourceId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(j =>
                j.Title.ToLower().Contains(term) ||
                j.CompanyName.ToLower().Contains(term) ||
                (j.Location != null && j.Location.ToLower().Contains(term)) ||
                j.Tags.ToLower().Contains(term));
        }

        // Apply status filter based on user-specific state
        var rawJobs = await query
            .OrderByDescending(j => j.FetchedAt)
            .ToListAsync(ct);

        var allJobs = rawJobs
            .OrderByDescending(j => j.PostedAt ?? new DateTimeOffset(j.FetchedAt))
            .ToList();

        IEnumerable<DiscoveredJob> filtered = allJobs;
        if (!string.IsNullOrWhiteSpace(status) && !status.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            if (status.Equals("Saved", StringComparison.OrdinalIgnoreCase))
            {
                filtered = filtered.Where(j => userStates.TryGetValue(j.Id, out var s) && s.Equals("Saved", StringComparison.OrdinalIgnoreCase));
            }
            else if (status.Equals("Dismissed", StringComparison.OrdinalIgnoreCase))
            {
                filtered = filtered.Where(j => userStates.TryGetValue(j.Id, out var s) && s.Equals("Dismissed", StringComparison.OrdinalIgnoreCase));
            }
            else if (status.Equals("New", StringComparison.OrdinalIgnoreCase))
            {
                filtered = filtered.Where(j => !userStates.TryGetValue(j.Id, out var s) || s.Equals("New", StringComparison.OrdinalIgnoreCase));
            }
        }

        var filteredList = filtered.ToList();
        var totalCount = filteredList.Count;

        var pagedItems = filteredList
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(j =>
            {
                var userState = userStates.TryGetValue(j.Id, out var s) ? s : "New";
                IReadOnlyList<string> tags;
                try
                {
                    tags = JsonSerializer.Deserialize<List<string>>(j.Tags) ?? new List<string>();
                }
                catch
                {
                    tags = Array.Empty<string>();
                }

                return new DiscoveredJobDto(
                    Id: j.Id,
                    SourceId: j.SourceId,
                    SourceName: j.Source?.Name ?? "External Feed",
                    ExternalId: j.ExternalId,
                    Title: j.Title,
                    CompanyName: j.CompanyName,
                    Location: j.Location,
                    Url: j.Url,
                    Description: j.Description,
                    PostedAt: j.PostedAt,
                    Tags: tags,
                    Status: userState,
                    FetchedAt: j.FetchedAt
                );
            })
            .ToList();

        return new DiscoveredJobListDto(pagedItems, totalCount, page, pageSize);
    }

    public async Task<IReadOnlyList<JobSourceDto>> GetJobSourcesAsync(CancellationToken ct = default)
    {
        return await _dbContext.JobSources
            .AsNoTracking()
            .OrderBy(s => s.Name)
            .Select(s => new JobSourceDto(
                s.Id,
                s.Name,
                s.Type,
                s.BaseUrl,
                s.Enabled,
                s.LastRunAt
            ))
            .ToListAsync(ct);
    }

    public async Task<ApplicationDetailDto> SaveJobToWishlistAsync(Guid discoveredJobId, CancellationToken ct = default)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue || userId.Value == Guid.Empty)
        {
            throw new UnauthorizedAccessException("User is not authenticated.");
        }

        var job = await _dbContext.DiscoveredJobs
            .Include(j => j.Source)
            .FirstOrDefaultAsync(j => j.Id == discoveredJobId, ct);

        if (job == null)
        {
            throw new KeyNotFoundException($"Discovered job '{discoveredJobId}' was not found.");
        }

        // Check if an application already exists for this discovered job
        var existingApp = await _dbContext.Applications
            .FirstOrDefaultAsync(a => a.UserId == userId.Value && a.DiscoveredJobId == discoveredJobId, ct);

        ApplicationDetailDto detailDto;

        if (existingApp != null)
        {
            detailDto = await _applicationService.GetByIdAsync(existingApp.Id, ct);
        }
        else
        {
            var createRequest = new CreateApplicationRequest(
                RoleTitle: job.Title,
                CompanyName: job.CompanyName,
                JobUrl: job.Url,
                Source: ApplicationSource.Other,
                SourceDetail: job.Source?.Name ?? "Job Discovery",
                Status: ApplicationStatus.Wishlist,
                Location: job.Location,
                JobDescription: job.Description,
                Notes: $"Imported from Job Discovery (Source: {job.Source?.Name ?? "External Feed"})."
            );

            detailDto = await _applicationService.CreateApplicationAsync(createRequest, ct);

            // Link DiscoveredJobId on the created application
            var appEntity = await _dbContext.Applications.FirstOrDefaultAsync(a => a.Id == detailDto.Id, ct);
            if (appEntity != null)
            {
                appEntity.DiscoveredJobId = discoveredJobId;
            }
        }

        // Update or insert UserDiscoveredJobState
        var stateRecord = await _dbContext.UserDiscoveredJobStates
            .FirstOrDefaultAsync(s => s.UserId == userId.Value && s.DiscoveredJobId == discoveredJobId, ct);

        if (stateRecord == null)
        {
            stateRecord = new UserDiscoveredJobState
            {
                Id = Guid.NewGuid(),
                UserId = userId.Value,
                DiscoveredJobId = discoveredJobId,
                State = "Saved",
                CreatedAt = DateTime.UtcNow
            };
            _dbContext.UserDiscoveredJobStates.Add(stateRecord);
        }
        else
        {
            stateRecord.State = "Saved";
            stateRecord.UpdatedAt = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync(ct);
        return detailDto;
    }

    public async Task<bool> DismissJobAsync(Guid discoveredJobId, CancellationToken ct = default)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue || userId.Value == Guid.Empty)
        {
            throw new UnauthorizedAccessException("User is not authenticated.");
        }

        var job = await _dbContext.DiscoveredJobs.FirstOrDefaultAsync(j => j.Id == discoveredJobId, ct);
        if (job == null)
        {
            throw new KeyNotFoundException($"Discovered job '{discoveredJobId}' was not found.");
        }

        var stateRecord = await _dbContext.UserDiscoveredJobStates
            .FirstOrDefaultAsync(s => s.UserId == userId.Value && s.DiscoveredJobId == discoveredJobId, ct);

        if (stateRecord == null)
        {
            stateRecord = new UserDiscoveredJobState
            {
                Id = Guid.NewGuid(),
                UserId = userId.Value,
                DiscoveredJobId = discoveredJobId,
                State = "Dismissed",
                CreatedAt = DateTime.UtcNow
            };
            _dbContext.UserDiscoveredJobStates.Add(stateRecord);
        }
        else
        {
            stateRecord.State = "Dismissed";
            stateRecord.UpdatedAt = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync(ct);
        return true;
    }

    public async Task<IngestJobsResultDto> IngestJobsAsync(CancellationToken ct = default)
    {
        // Ensure default JobSource exists
        var defaultSource = await _dbContext.JobSources.FirstOrDefaultAsync(s => s.Type == "Rss", ct);
        if (defaultSource == null)
        {
            defaultSource = new JobSource
            {
                Id = Guid.NewGuid(),
                Name = "Tech Engineering Feed",
                Type = "Rss",
                BaseUrl = "https://pipeline.local/feeds/engineering.rss",
                Config = "{\"sourceKey\":\"sample-tech-feed\"}",
                Enabled = true,
                CreatedAt = DateTime.UtcNow
            };
            _dbContext.JobSources.Add(defaultSource);
            await _dbContext.SaveChangesAsync(ct);
        }

        var enabledSources = await _dbContext.JobSources.Where(s => s.Enabled).ToListAsync(ct);
        var existingHashesList = await _dbContext.DiscoveredJobs.Select(j => j.Hash).ToListAsync(ct);
        var existingHashes = existingHashesList.ToHashSet();

        int totalFetched = 0;
        int newJobsStored = 0;
        int duplicatesSkipped = 0;

        foreach (var source in enabledSources)
        {
            // Find connector matching source config or default fallback
            var connector = _connectors.FirstOrDefault(c =>
                source.Config.Contains(c.SourceKey, StringComparison.OrdinalIgnoreCase))
                ?? _connectors.FirstOrDefault();

            if (connector == null) continue;

            IReadOnlyList<RawJob> rawJobs;
            try
            {
                rawJobs = await connector.FetchAsync(source, ct);
            }
            catch
            {
                continue;
            }

            totalFetched += rawJobs.Count;

            foreach (var raw in rawJobs)
            {
                var hashInput = $"{raw.CompanyName.Trim().ToLowerInvariant()}:{raw.Title.Trim().ToLowerInvariant()}:{raw.Location?.Trim().ToLowerInvariant() ?? ""}";
                var hash = ComputeHash(hashInput);

                if (existingHashes.Contains(hash))
                {
                    duplicatesSkipped++;
                    continue;
                }

                var entity = new DiscoveredJob
                {
                    Id = Guid.NewGuid(),
                    SourceId = source.Id,
                    ExternalId = raw.ExternalId,
                    Title = raw.Title,
                    CompanyName = raw.CompanyName,
                    Location = raw.Location,
                    Url = raw.Url,
                    Description = raw.Description,
                    PostedAt = raw.PostedAt,
                    Tags = JsonSerializer.Serialize(raw.Tags),
                    Hash = hash,
                    FetchedAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow
                };

                _dbContext.DiscoveredJobs.Add(entity);
                existingHashes.Add(hash);
                newJobsStored++;
            }

            source.LastRunAt = DateTime.UtcNow;
        }

        if (newJobsStored > 0 || enabledSources.Count > 0)
        {
            await _dbContext.SaveChangesAsync(ct);
        }

        return new IngestJobsResultDto(totalFetched, newJobsStored, duplicatesSkipped);
    }

    private static string ComputeHash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
