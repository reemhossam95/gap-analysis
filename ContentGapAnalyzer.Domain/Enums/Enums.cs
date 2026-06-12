namespace ContentGapAnalyzer.Domain.Enums;

public enum GapReportStatus
{
    Pending = 0,
    Processing = 1,
    Completed = 2,
    Failed = 3
}

public enum OpportunityDifficulty
{
    EasyWin = 0,
    Medium = 1,
    Hard = 2,
    Golden = 3
}

public enum VideoCategory
{
    Technology = 0,
    AIAndML = 1,
    WebDevelopment = 2,
    Design = 3,
    Backend = 4,
    Cybersecurity = 5,
    General = 99
}
