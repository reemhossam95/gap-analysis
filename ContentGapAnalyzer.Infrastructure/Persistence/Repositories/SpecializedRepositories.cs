using ContentGapAnalyzer.Domain.Entities;
using ContentGapAnalyzer.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ContentGapAnalyzer.Infrastructure.Persistence.Repositories;

public class VideoRepository : Repository<Video>, IVideoRepository
{
    public VideoRepository(AppDbContext context) : base(context) { }

    public async Task<Video?> GetByVideoIdAsync(string videoId, CancellationToken cancellationToken = default)
        => await _dbSet.FirstOrDefaultAsync(v => v.VideoId == videoId && !v.IsDeleted, cancellationToken);

    public async Task<IReadOnlyList<Video>> GetByChannelIdAsync(
        string channelId, CancellationToken cancellationToken = default)
        => await _dbSet
            .Where(v => v.ChannelId == channelId && !v.IsDeleted)
            .OrderByDescending(v => v.PublishedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Video>> GetByCategoryAsync(
        string category, int maxResults, CancellationToken cancellationToken = default)
        => await _dbSet
            .Where(v => v.Category == category && !v.IsDeleted)
            .OrderByDescending(v => v.ViewCount)
            .Take(maxResults)
            .ToListAsync(cancellationToken);

    public async Task<(IReadOnlyList<Video> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.Where(v => !v.IsDeleted).OrderByDescending(v => v.PublishedAt);
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return (items, totalCount);
    }
}

public class GapReportRepository : Repository<GapReport>, IGapReportRepository
{
    public GapReportRepository(AppDbContext context) : base(context) { }

    public async Task<GapReport?> GetByVideoIdAsync(string videoId, CancellationToken cancellationToken = default)
        => await _dbSet
            .Where(g => g.VideoId == videoId && !g.IsDeleted)
            .OrderByDescending(g => g.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<GapReport>> GetRecentAsync(
        int count, CancellationToken cancellationToken = default)
        => await _dbSet
            .Where(g => !g.IsDeleted)
            .OrderByDescending(g => g.CreatedAt)
            .Take(count)
            .ToListAsync(cancellationToken);
}

public class AnalysisSessionRepository : Repository<AnalysisSession>, IAnalysisSessionRepository
{
    public AnalysisSessionRepository(AppDbContext context) : base(context) { }

    public async Task<IReadOnlyList<AnalysisSession>> GetByGapReportIdAsync(
        int gapReportId, CancellationToken cancellationToken = default)
        => await _dbSet
            .Where(s => s.GapReportId == gapReportId && !s.IsDeleted)
            .OrderByDescending(s => s.SessionDate)
            .ToListAsync(cancellationToken);

    public async Task<AnalysisSession?> GetBySessionIdAsync(
        Guid sessionId, CancellationToken cancellationToken = default)
        => await _dbSet.FirstOrDefaultAsync(s => s.SessionId == sessionId && !s.IsDeleted, cancellationToken);
}

public class CachedTrendResultRepository : Repository<CachedTrendResult>, ICachedTrendResultRepository
{
    public CachedTrendResultRepository(AppDbContext context) : base(context) { }

    public async Task<CachedTrendResult?> GetByCacheKeyAsync(
        string cacheKey, CancellationToken cancellationToken = default)
        => await _dbSet.FirstOrDefaultAsync(
            c => c.CacheKey == cacheKey && !c.IsDeleted && c.ExpiresAt > DateTime.UtcNow,
            cancellationToken);

    public async Task DeleteExpiredAsync(CancellationToken cancellationToken = default)
    {
        var expired = await _dbSet
            .Where(c => c.ExpiresAt <= DateTime.UtcNow)
            .ToListAsync(cancellationToken);

        _dbSet.RemoveRange(expired);
    }
}

public class OpportunityRepository : Repository<Opportunity>, IOpportunityRepository
{
    public OpportunityRepository(AppDbContext context) : base(context) { }

    public async Task<IReadOnlyList<Opportunity>> GetByKeywordAsync(
        string keyword, CancellationToken cancellationToken = default)
        => await _dbSet
            .Where(o => EF.Functions.Like(o.Keyword, $"%{keyword}%") && !o.IsDeleted)
            .OrderByDescending(o => o.GapScore)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Opportunity>> GetTopOpportunitiesAsync(
        int count, string? region = null, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.Where(o => !o.IsDeleted);
        if (!string.IsNullOrEmpty(region))
            query = query.Where(o => o.Region == region);

        return await query
            .OrderByDescending(o => o.GapScore)
            .Take(count)
            .ToListAsync(cancellationToken);
    }
}
