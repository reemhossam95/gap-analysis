using System;
using System.Collections.Generic;

namespace ContentGapAnalyzer.Domain.Entities;

public class VideoAnalysis
{
    public int Id { get; set; }
    public string VideoId { get; set; } = string.Empty;

    // الحقول التحليلية الأساسية
    public double CompetitionDifficulty { get; set; }
    public double OpportunityScore { get; set; }
    public double TrendGrowth { get; set; }

    // القوائم التحليلية (ستُخزن غالباً كـ JSON في قاعدة البيانات)
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

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}