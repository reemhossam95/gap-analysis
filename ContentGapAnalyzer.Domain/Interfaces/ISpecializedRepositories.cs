using ContentGapAnalyzer.Domain.Entities;

namespace ContentGapAnalyzer.Domain.Interfaces;

public interface IVideoRepository : IRepository<Video>
{
    Task<Video?> GetByVideoIdAsync(string videoId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Video>> GetByChannelIdAsync(string channelId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Video>> GetByCategoryAsync(string category, int maxResults, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Video> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default);
}

public interface IGapReportRepository : IRepository<GapReport>
{
    Task<GapReport?> GetByVideoIdAsync(string videoId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<GapReport>> GetRecentAsync(int count, CancellationToken cancellationToken = default);
}

public interface IAnalysisSessionRepository : IRepository<AnalysisSession>
{
    Task<IReadOnlyList<AnalysisSession>> GetByGapReportIdAsync(int gapReportId, CancellationToken cancellationToken = default);
    Task<AnalysisSession?> GetBySessionIdAsync(Guid sessionId, CancellationToken cancellationToken = default);
}

public interface ICachedTrendResultRepository : IRepository<CachedTrendResult>
{
    Task<CachedTrendResult?> GetByCacheKeyAsync(string cacheKey, CancellationToken cancellationToken = default);
    Task DeleteExpiredAsync(CancellationToken cancellationToken = default);
}

public interface IOpportunityRepository : IRepository<Opportunity>
{
    Task<IReadOnlyList<Opportunity>> GetByKeywordAsync(string keyword, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Opportunity>> GetTopOpportunitiesAsync(int count, string? region = null, CancellationToken cancellationToken = default);
}
