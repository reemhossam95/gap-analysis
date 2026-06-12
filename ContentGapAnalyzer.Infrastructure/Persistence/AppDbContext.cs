using ContentGapAnalyzer.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ContentGapAnalyzer.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // Existing tables — mapped exactly to the existing AutotubeDB schema
    public DbSet<Channel> Channels => Set<Channel>();
    public DbSet<Video> Videos => Set<Video>();
    public DbSet<AnalyticsSnapshot> AnalyticsSnapshots => Set<AnalyticsSnapshot>();
    public DbSet<HistoricalStatistic> HistoricalStatistics => Set<HistoricalStatistic>();

    // New tables added by this backend (safe migrations only)
    public DbSet<GapReport> GapReports => Set<GapReport>();
    public DbSet<AnalysisSession> AnalysisSessions => Set<AnalysisSession>();
    public DbSet<CachedTrendResult> CachedTrendResults => Set<CachedTrendResult>();
    public DbSet<Opportunity> Opportunities => Set<Opportunity>();

    public DbSet<VideoAnalysis> VideoAnalyses => Set<VideoAnalysis>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Added or EntityState.Modified);

        foreach (var entry in entries)
        {
            if (entry.Entity is Domain.Common.BaseEntity entity)
            {
                if (entry.State == EntityState.Added)
                    entity.CreatedAt = DateTime.UtcNow;

                entity.UpdatedAt = DateTime.UtcNow;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
