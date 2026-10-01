using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Pipeline.Application.Common.Interfaces;
using Pipeline.Domain.Common;
using Pipeline.Domain.Entities;
using JobApplication = Pipeline.Domain.Entities.Application;

namespace Pipeline.Infrastructure.Persistence;

public class PipelineDbContext : IdentityDbContext<User, IdentityRole<Guid>, Guid>
{
    private readonly ICurrentUserService _currentUserService;

    public PipelineDbContext(
        DbContextOptions<PipelineDbContext> options,
        ICurrentUserService currentUserService)
        : base(options)
    {
        _currentUserService = currentUserService;
    }

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<JobApplication> Applications => Set<JobApplication>();
    public DbSet<ApplicationStatusHistory> ApplicationStatusHistories => Set<ApplicationStatusHistory>();
    public DbSet<Contact> Contacts => Set<Contact>();
    public DbSet<ApplicationContact> ApplicationContacts => Set<ApplicationContact>();
    public DbSet<Interaction> Interactions => Set<Interaction>();
    public DbSet<Interview> Interviews => Set<Interview>();
    public DbSet<InterviewContact> InterviewContacts => Set<InterviewContact>();
    public DbSet<InterviewQuestion> InterviewQuestions => Set<InterviewQuestion>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<DocumentVersion> DocumentVersions => Set<DocumentVersion>();
    public DbSet<JobReference> JobReferences => Set<JobReference>();
    public DbSet<ApplicationReference> ApplicationReferences => Set<ApplicationReference>();
    public DbSet<TaskItem> Tasks => Set<TaskItem>();
    public DbSet<Reminder> Reminders => Set<Reminder>();
    public DbSet<EmailTemplate> EmailTemplates => Set<EmailTemplate>();
    public DbSet<PushSubscriptionEntity> PushSubscriptions => Set<PushSubscriptionEntity>();
    public DbSet<CustomStage> CustomStages => Set<CustomStage>();
    public DbSet<JobSource> JobSources => Set<JobSource>();
    public DbSet<DiscoveredJob> DiscoveredJobs => Set<DiscoveredJob>();
    public DbSet<UserDiscoveredJobState> UserDiscoveredJobStates => Set<UserDiscoveredJobState>();
    public DbSet<UserIntegrationSetting> UserIntegrationSettings => Set<UserIntegrationSetting>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // User configuration
        builder.Entity<User>(b =>
        {
            b.HasIndex(u => u.CalendarFeedToken).IsUnique();
        });

        // User integration settings configuration
        builder.Entity<UserIntegrationSetting>(b =>
        {
            b.HasIndex(u => u.UserId).IsUnique();
            b.HasOne(u => u.User)
                .WithMany()
                .HasForeignKey(u => u.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Application relations
        builder.Entity<JobApplication>(b =>
        {
            b.HasIndex(a => new { a.UserId, a.Status });
            b.HasIndex(a => new { a.UserId, a.StatusChangedAt });
            b.HasOne(a => a.Company)
                .WithMany(c => c.Applications)
                .HasForeignKey(a => a.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(a => a.DocumentVersionCv)
                .WithMany()
                .HasForeignKey(a => a.DocumentVersionCvId)
                .OnDelete(DeleteBehavior.SetNull);

            b.HasOne(a => a.DocumentVersionCover)
                .WithMany()
                .HasForeignKey(a => a.DocumentVersionCoverId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Contact relations
        builder.Entity<Contact>(b =>
        {
            b.HasIndex(c => new { c.UserId, c.NextFollowUpAt });
            b.HasOne(c => c.Company)
                .WithMany(comp => comp.Contacts)
                .HasForeignKey(c => c.CompanyId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Interview relations
        builder.Entity<Interview>(b =>
        {
            b.HasIndex(i => new { i.UserId, i.ScheduledAt });
            b.HasOne(i => i.Application)
                .WithMany(a => a.Interviews)
                .HasForeignKey(i => i.ApplicationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Document relations
        builder.Entity<Document>(b =>
        {
            b.HasMany(d => d.Versions)
                .WithOne(v => v.Document)
                .HasForeignKey(v => v.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure global query filters for IUserOwnedEntity and ISoftDeletable
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            var clrType = entityType.ClrType;
            var isUserOwned = typeof(IUserOwnedEntity).IsAssignableFrom(clrType);
            var isSoftDeletable = typeof(ISoftDeletable).IsAssignableFrom(clrType);

            if (isUserOwned || isSoftDeletable)
            {
                var parameter = Expression.Parameter(clrType, "e");
                Expression? filter = null;

                if (isUserOwned)
                {
                    // e => e.UserId == _currentUserService.UserId
                    var userIdProp = Expression.Property(parameter, nameof(IUserOwnedEntity.UserId));
                    var currentUserIdProp = Expression.Property(Expression.Constant(this), nameof(CurrentUserId));
                    filter = Expression.Equal(userIdProp, currentUserIdProp);
                }

                if (isSoftDeletable)
                {
                    // e => e.DeletedAt == null
                    var deletedAtProp = Expression.Property(parameter, nameof(ISoftDeletable.DeletedAt));
                    var nullConstant = Expression.Constant(null, typeof(DateTime?));
                    var softDeleteFilter = Expression.Equal(deletedAtProp, nullConstant);

                    filter = filter != null
                        ? Expression.AndAlso(filter, softDeleteFilter)
                        : softDeleteFilter;
                }

                if (filter != null)
                {
                    var lambda = Expression.Lambda(filter, parameter);
                    entityType.SetQueryFilter(lambda);
                }
            }
        }
    }

    public Guid CurrentUserId => _currentUserService.UserId ?? Guid.Empty;

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var currentUserId = _currentUserService.UserId;

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Entity.CreatedAt == default)
                    entry.Entity.CreatedAt = now;
                entry.Entity.UpdatedAt = now;

                if (entry.Entity is IUserOwnedEntity userOwned && currentUserId.HasValue && userOwned.UserId == Guid.Empty)
                {
                    userOwned.UserId = currentUserId.Value;
                }
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
