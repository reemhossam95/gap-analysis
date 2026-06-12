using ContentGapAnalyzer.Domain.Common;

namespace ContentGapAnalyzer.Domain.Entities;

public class Channel : BaseEntity
{
    public string ChannelId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ThumbnailUrl { get; set; } = string.Empty;
    public long SubscriberCount { get; set; }
    public long TotalViews { get; set; }
    public long VideoCount { get; set; }

    // Navigation properties
    public ICollection<Video> Videos { get; set; } = new List<Video>();
    public ICollection<AnalyticsSnapshot> AnalyticsSnapshots { get; set; } = new List<AnalyticsSnapshot>();
    public ICollection<HistoricalStatistic> HistoricalStatistics { get; set; } = new List<HistoricalStatistic>();
}
