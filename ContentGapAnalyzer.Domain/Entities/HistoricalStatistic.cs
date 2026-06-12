using ContentGapAnalyzer.Domain.Common;

namespace ContentGapAnalyzer.Domain.Entities;

public class HistoricalStatistic : BaseEntity
{
    public string ChannelId { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public long Views { get; set; }
    public long Subscribers { get; set; }
    public long WatchTimeMinutes { get; set; }
    public double EngagementRate { get; set; }
    public double ClickThroughRate { get; set; }

    // Navigation
    public Channel? Channel { get; set; }
}
