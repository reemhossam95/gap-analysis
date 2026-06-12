using ContentGapAnalyzer.Domain.Common;

namespace ContentGapAnalyzer.Domain.Entities;

public class CachedTrendResult : BaseEntity
{
    public string CacheKey { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Keywords { get; set; } = string.Empty;
    public string ResultJson { get; set; } = "[]";
    public DateTime ExpiresAt { get; set; }
    public int HitCount { get; set; } = 0;
}
