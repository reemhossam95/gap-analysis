using ContentGapAnalyzer.Domain.Common;

namespace ContentGapAnalyzer.Domain.Entities;

public class Video : BaseEntity
{
    public string VideoId { get; set; } = string.Empty;
    public string ChannelId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ThumbnailUrl { get; set; } = string.Empty;
    public long ViewCount { get; set; }
    public long LikeCount { get; set; }
    public long CommentCount { get; set; }
    public long WatchTimeMinutes { get; set; }
    public double ClickThroughRate { get; set; }
    public double AverageViewDuration { get; set; }
    public DateTime PublishedAt { get; set; }
    public string Category { get; set; } = string.Empty;

    // Navigation
    public Channel? Channel { get; set; }

    public ICollection<GapReport> GapReports { get; set; } = new List<GapReport>();
}
