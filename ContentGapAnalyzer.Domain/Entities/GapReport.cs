using ContentGapAnalyzer.Domain.Common;
using ContentGapAnalyzer.Domain.Enums;

namespace ContentGapAnalyzer.Domain.Entities;

public class GapReport : BaseEntity
{
    public string VideoId { get; set; } = string.Empty;
    public string VideoTitle { get; set; } = string.Empty;
    public string ChannelId { get; set; } = string.Empty;
    public GapReportStatus Status { get; set; } = GapReportStatus.Pending;

    // AI-Generated content stored as JSON
    public string ContentGapsJson { get; set; } = "[]";
    public string AudiencePainPointsJson { get; set; } = "[]";
    public string MissedOpportunitiesJson { get; set; } = "[]";
    public string WeaknessesJson { get; set; } = "[]";
    public string StrengthsJson { get; set; } = "[]";
    public string SeoRecommendationsJson { get; set; } = "[]";
    public string CtrOptimizationSuggestionsJson { get; set; } = "[]";
    public string HookImprovementsJson { get; set; } = "[]";
    public string RetentionImprovementsJson { get; set; } = "[]";
    public string ViralPotentialAnalysis { get; set; } = string.Empty;
    public string RawAiResponse { get; set; } = string.Empty;

    // Metrics at time of analysis
    public double CompetitionDifficulty { get; set; }
    public double OpportunityScore { get; set; }
    public double TrendGrowth { get; set; }

    public Video? Video { get; set; }
    public ICollection<AnalysisSession> AnalysisSessions { get; set; } = new List<AnalysisSession>();
}
