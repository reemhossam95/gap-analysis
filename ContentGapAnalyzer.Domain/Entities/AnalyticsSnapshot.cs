using ContentGapAnalyzer.Domain.Common;

namespace ContentGapAnalyzer.Domain.Entities;

public class AnalyticsSnapshot : BaseEntity
{
    public string ChannelId { get; set; } = string.Empty;
    public long SubscriberCount { get; set; }
    public long TotalViews { get; set; }
    public long WatchTimeMinutes { get; set; }
    public double AvgEngagementRate { get; set; }
    public double AvgClickThroughRate { get; set; }
    public long NewSubscribers { get; set; }
    public long NewViews { get; set; }
    public DateTime RecordedAt { get; set; }
    public Channel? Channel { get; set; }
}
