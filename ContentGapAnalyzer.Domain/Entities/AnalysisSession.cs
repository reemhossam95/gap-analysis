using ContentGapAnalyzer.Domain.Common;

namespace ContentGapAnalyzer.Domain.Entities;

public class AnalysisSession : BaseEntity
{
    public Guid SessionId { get; set; } = Guid.NewGuid();
    public int GapReportId { get; set; }
    public string VideoId { get; set; } = string.Empty;
    public string ContextJson { get; set; } = "{}";
    public string Notes { get; set; } = string.Empty;
    public DateTime SessionDate { get; set; } = DateTime.UtcNow;

    // Navigation
    public GapReport? GapReport { get; set; }
}
