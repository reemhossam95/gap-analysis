using ContentGapAnalyzer.Domain.Enums;
using System.Text.Json.Serialization;
namespace ContentGapAnalyzer.Application.DTOs;

public record VideoDto(
    int Id,
    string VideoId,
    string ChannelId,
    string Title,
    string Description,
    string ThumbnailUrl,
    long ViewCount,
    long LikeCount,
    long CommentCount,
    long WatchTimeMinutes,
    double ClickThroughRate,
    double AverageViewDuration,
    DateTime PublishedAt,
    string Category,
    DateTime CreatedAt
);

public record ChannelDto(
    int Id,
    string ChannelId,
    string Title,
    string Description,
    string ThumbnailUrl,
    long SubscriberCount,
    long TotalViews,
    long VideoCount,
    DateTime CreatedAt
);

public record GapReportDto(
    int Id,
    string VideoId,
    string VideoTitle,
    string ChannelId,
    GapReportStatus Status,
    List<string> ContentGaps,
    List<string> AudiencePainPoints,
    List<string> MissedOpportunities,
    List<string> Weaknesses,
    List<string> Strengths,
    List<string> SeoRecommendations,
    List<string> CtrOptimizationSuggestions,
    List<string> HookImprovements,
    List<string> RetentionImprovements,
    string ViralPotentialAnalysis,
    double CompetitionDifficulty,
    double OpportunityScore,
    double TrendGrowth,
    DateTime CreatedAt
);

public record AnalysisSessionDto(
    int Id,
    Guid SessionId,
    int GapReportId,
    string VideoId,
    string Notes,
    DateTime SessionDate,
    DateTime CreatedAt
);

public record OpportunityDto(
    int Id,
    string Keyword,
    string Category,
    double GapScore,
    double DemandScore,
    double CompetitionScore,
    double TrendScore,
    long SearchVolume,
    long AvgViews,
    string Difficulty,
    List<string> Tags,
    string Region,
    DateTime AnalyzedAt
);

public record TrendingVideoDto(
    string VideoId,
    string Title,
    string ChannelId,
    string ChannelTitle,
    string ThumbnailUrl,
    long ViewCount,
    long LikeCount,
    long CommentCount,
    DateTime PublishedAt,
    string Category,
    double GapScore,
    double DemandScore,
    double CompetitionScore,
    double TrendScore
);

public record DiscoveryDashboardStatsDto(
    long TopicsAnalyzed,
    long EasyWinsFound,
    double AvgGapScore,
    long HighGrowthChannels
);

public record AnalyticsSnapshotDto(
    int Id,
    string ChannelId,
    long SubscriberCount,
    long TotalViews,
    long WatchTimeMinutes,
    double AvgEngagementRate,
    double AvgClickThroughRate,
    long NewSubscribers,
    long NewViews,
    DateTime RecordedAt
);

// --- الإضافات الجديدة المطلوبة لـ Handler ---
public record VideoBasicInfo(string VideoId, string Title);

public record GapAnalysisInput(VideoBasicInfo TargetVideo, List<VideoBasicInfo> CompetitorVideos);

// --- المدخلات والمخرجات المطلوبة لخدمة Gemini ---

public record VideoMetricsInput(
    string Title, 
    string Category, 
    long ViewCount, 
    long LikeCount, 
    long CommentCount, 
    double ClickThroughRate, 
    DateTime PublishedAt
);

public record VideoMetricsAnalysis(
    double CompetitionDifficulty,
    double OpportunityScore,
    double TrendGrowth,
    string Reasoning
);

public record GapAnalysisResult(
    List<string> ContentGaps,
    List<string> AudiencePainPoints,
    List<string> MissedOpportunities,
    List<string> Weaknesses,
    List<string> Strengths,
    List<string> SeoRecommendations,
    List<string> CtrOptimizationSuggestions,
    List<string> HookImprovements,
    List<string> RetentionImprovements,
    string ViralPotentialAnalysis,
    double CompetitionDifficulty,
    double OpportunityScore,
    double TrendGrowth
);

// --- الإضافة الجديدة للتقرير التجميعي ---
public record AggregateReport(
    [property: JsonPropertyName("immediateActions")] List<string> ImmediateActions, // خطوات تنفيذية خلال 30 يوم
    [property: JsonPropertyName("contentStrategy")] List<string> ContentStrategy,   // استراتيجية المحتوى والنمو
    [property: JsonPropertyName("retentionTactics")] List<string> RetentionTactics, // أساليب الحفاظ على الجمهور
    [property: JsonPropertyName("growthOpportunities")] List<string> GrowthOpportunities, // الفرص المتاحة للتوسع
    [property: JsonPropertyName("executiveSummary")] string ExecutiveSummary       // الخلاصة التنفيذية
);    
    