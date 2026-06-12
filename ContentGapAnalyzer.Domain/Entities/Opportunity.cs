using ContentGapAnalyzer.Domain.Common;
using ContentGapAnalyzer.Domain.Enums;

namespace ContentGapAnalyzer.Domain.Entities;

public class Opportunity : BaseEntity
{
    public string Keyword { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public double GapScore { get; set; }
    public double DemandScore { get; set; }
    public double CompetitionScore { get; set; }
    public double TrendScore { get; set; }
    public long SearchVolume { get; set; }
    public long AvgViews { get; set; }
    public OpportunityDifficulty Difficulty { get; set; } = OpportunityDifficulty.Medium;
    public string OpportunityTagsJson { get; set; } = "[]";
    public string Region { get; set; } = string.Empty;
    public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;
}
