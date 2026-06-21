using System;
using System.Collections.Generic;
using ContentGapAnalyzer.Domain.Common;

namespace ContentGapAnalyzer.Domain.Entities;
public class VideoAnalysis : BaseEntity
{
    public string VideoId { get; set; } = string.Empty;

    public double CompetitionDifficulty { get; set; }
    public double OpportunityScore { get; set; }
    public double TrendGrowth { get; set; }

    public List<string> ContentGaps { get; set; } = new();
    public List<string> AudiencePainPoints { get; set; } = new();
    public List<string> MissedOpportunities { get; set; } = new();
    public List<string> Weaknesses { get; set; } = new();
    public List<string> Strengths { get; set; } = new();
    public List<string> SeoRecommendations { get; set; } = new();
    public List<string> CtrOptimizationSuggestions { get; set; } = new();
    public List<string> HookImprovements { get; set; } = new();
    public List<string> RetentionImprovements { get; set; } = new();

    public string ViralPotentialAnalysis { get; set; } = string.Empty;
}