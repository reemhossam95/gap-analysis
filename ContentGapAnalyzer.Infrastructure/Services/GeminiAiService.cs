using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ContentGapAnalyzer.Application.DTOs;
using ContentGapAnalyzer.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace ContentGapAnalyzer.Infrastructure.Services;

public class GeminiAiService : IGeminiAiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GeminiAiService> _logger;
    private readonly List<string> _apiKeys;
    private int _keyIndex = 0;
    private const string Model = "gemini-2.5-flash";
    private const string BaseUrl = "https://generativelanguage.googleapis.com/v1beta/models";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    public GeminiAiService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<GeminiAiService> logger)
    {
        _httpClient = httpClientFactory.CreateClient("Gemini");
        _logger = logger;

        _apiKeys = configuration.GetSection("Gemini:ApiKeys").Get<List<string>>()
            ?? throw new InvalidOperationException("Gemini:ApiKeys is not configured.");

        if (_apiKeys.Count == 0)
            throw new InvalidOperationException("No Gemini API keys found.");
    }

    public async Task<VideoMetricsAnalysis> AnalyzeVideoMetricsAsync(
        VideoMetricsInput input,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var ageInDays = (DateTime.UtcNow - input.PublishedAt).TotalDays;
            var viewsPerDay = ageInDays > 0 ? input.ViewCount / ageInDays : input.ViewCount;

            var engagementRate = input.ViewCount > 0
                ? ((double)(input.LikeCount + input.CommentCount) / input.ViewCount) * 100
                : 0;

            var prompt = BuildMetricsPrompt(input, ageInDays, viewsPerDay, engagementRate);

            var responseText = await CallGeminiAsync(prompt, CancellationToken.None);
            var cleaned = Sanitize(responseText);
            _logger.LogWarning("RAW GEMINI RESPONSE:");
            _logger.LogWarning(responseText);

            _logger.LogWarning("CLEANED RESPONSE:");
            _logger.LogWarning(cleaned);
            var parsed = JsonSerializer.Deserialize<InternalMetricsResponse>(cleaned, JsonOptions);

            if (parsed is null)
                throw new JsonException("Invalid Gemini metrics response");

            return new VideoMetricsAnalysis(
                CompetitionDifficulty: Clamp(parsed.CompetitionDifficulty),
                OpportunityScore: Clamp(parsed.OpportunityScore),
                TrendGrowth: Clamp(parsed.TrendGrowth),
                Reasoning: parsed.Reasoning ?? string.Empty
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gemini failed to analyze video: {Title}. Returning default metrics.", input.Title);
            
            return new VideoMetricsAnalysis(
                CompetitionDifficulty: 0,
                OpportunityScore: 0,
                TrendGrowth: 0,
                Reasoning: "Analysis temporarily unavailable due to service timeout."
            );
        }
    }

    public async Task<GapAnalysisResult> GenerateGapAnalysisAsync(
        GapAnalysisInput input,
        CancellationToken cancellationToken = default)
    {
        var prompt = BuildGapPrompt(input);

        var responseText = await CallGeminiAsync(prompt, cancellationToken);
        var cleaned = Sanitize(responseText);

        var parsed = JsonSerializer.Deserialize<InternalGapAnalysisResponse>(cleaned, JsonOptions);

        if (parsed is null)
            throw new JsonException("Invalid Gemini gap analysis response");

        return new GapAnalysisResult(
            ContentGaps: parsed.ContentGaps ?? new(),
            AudiencePainPoints: parsed.AudiencePainPoints ?? new(),
            MissedOpportunities: parsed.MissedOpportunities ?? new(),
            Weaknesses: parsed.Weaknesses ?? new(),
            Strengths: parsed.Strengths ?? new(),
            SeoRecommendations: parsed.SeoRecommendations ?? new(),
            CtrOptimizationSuggestions: parsed.CtrOptimizationSuggestions ?? new(),
            HookImprovements: parsed.HookImprovements ?? new(),
            RetentionImprovements: parsed.RetentionImprovements ?? new(),
            ViralPotentialAnalysis: parsed.ViralPotentialAnalysis ?? string.Empty,
            CompetitionDifficulty: Clamp(parsed.CompetitionDifficulty),
            OpportunityScore: Clamp(parsed.OpportunityScore),
            TrendGrowth: Clamp(parsed.TrendGrowth)
        );
    }

    public async Task<AggregateReport> GenerateAggregateReportAsync(List<GapAnalysisResult> previousResults, CancellationToken cancellationToken = default)
    {
        var summaryContext = JsonSerializer.Serialize(previousResults);

        var prompt = $@"You are an expert YouTube Strategist. I have analyzed {previousResults.Count} videos.
        Here is the data from all previous analyses: {summaryContext}

        TASK: Perform a strategic synthesis and provide a COMPREHENSIVE ROADMAP.
        1. ImmediateActions: Define actionable steps for the next 30 days (Thumbnails, Hooks).
        2. ContentStrategy: Define content patterns to focus on to increase TrendGrowth.
        3. RetentionTactics: Propose methods to reduce drop-off based on AudiencePainPoints.
        4. GrowthOpportunities: Identify competitor gaps to exploit.
        5. ExecutiveSummary: Summarize the overall strategic direction in two sentences.

        Return ONLY JSON matching this exact schema:
        {{ 
            ""immediateActions"": [], ""contentStrategy"": [], ""retentionTactics"": [], ""growthOpportunities"": [], ""executiveSummary"": ""string"" 
        }}";

        var responseText = await CallGeminiAsync(prompt, cancellationToken);
        string cleaned = Sanitize(responseText);

        try
        {
            return JsonSerializer.Deserialize<AggregateReport>(cleaned, JsonOptions) ?? CreateEmptyReport();
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize AggregateReport. Raw response: {Raw}", cleaned);
            
            // محاولة أخيرة للإصلاح القسري للنص المقطوع
            try 
            { 
                string forceFixed = cleaned.TrimEnd().TrimEnd(']').TrimEnd('}').TrimEnd('"') + "\"} }";
                return JsonSerializer.Deserialize<AggregateReport>(forceFixed, JsonOptions) ?? CreateEmptyReport(); 
            }
            catch { return CreateEmptyReport(); }
        }
    }

    private AggregateReport CreateEmptyReport() => 
        new AggregateReport(new List<string>(), new List<string>(), new List<string>(), new List<string>(), "Analysis currently unavailable due to data parsing error.");

    private async Task<string> CallGeminiAsync(string prompt, CancellationToken cancellationToken)
    {
        var body = new
        {
            contents = new[]
            {
                new { parts = new[] { new { text = prompt } } }
            },
            generationConfig = new
            {
                temperature = 0.0,
                topK = 40,
                topP = 0.95,
                maxOutputTokens = 4096,
                responseMimeType = "application/json"
            }
        };

        var json = JsonSerializer.Serialize(body);

        for (int i = 0; i < _apiKeys.Count; i++)
        {
            var key = _apiKeys[(_keyIndex + i) % _apiKeys.Count];
            var url = $"{BaseUrl}/{Model}:generateContent?key={key}";

            using var request = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, request, cancellationToken);
            var responseText = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _keyIndex = (_keyIndex + i + 1) % _apiKeys.Count;
                var parsed = JsonSerializer.Deserialize<GeminiApiResponseWrapper>(responseText, JsonOptions);
                var text = parsed?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;

                if (string.IsNullOrWhiteSpace(text))
                    throw new InvalidOperationException("Empty Gemini response");

                return text;
            }

            if ((int)response.StatusCode == 429)
            {
                _logger.LogWarning("Gemini quota exceeded for key index {KeyIndex}. Waiting...", (_keyIndex + i) % _apiKeys.Count);
                await Task.Delay(2000, cancellationToken); 
                continue;
            }

            _logger.LogError("Gemini error: {Status} - {Body}", response.StatusCode, responseText);
            throw new HttpRequestException(responseText);
        }

        throw new InvalidOperationException("All Gemini API keys exceeded quota.");
    }

    private static string Sanitize(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return string.Empty;

        // 1. إزالة الـ Markdown
        raw = raw.Replace("```json", "").Replace("```", "").Trim();

        // 2. إصلاح القطع في نهاية الـ JSON
        if (!raw.EndsWith("}"))
        {
            if (raw.EndsWith("]")) raw += "}";
            else raw += "\"} }"; 
        }

        return raw;
    }

    private static double Clamp(double value) => Math.Clamp(value, 0, 100);

    private static string BuildMetricsPrompt(VideoMetricsInput input, double ageInDays, double viewsPerDay, double engagementRate) =>
        $@"Analyze YouTube video metrics and return ONLY a JSON object matching this schema: {{ ""competitionDifficulty"": 0.0, ""opportunityScore"": 0.0, ""trendGrowth"": 0.0, ""reasoning"": ""string"" }}
        Title: {input.Title}, Category: {input.Category}, Views: {input.ViewCount}, Likes: {input.LikeCount}, Comments: {input.CommentCount}, CTR: {input.ClickThroughRate}, AgeDays: {ageInDays}, Velocity: {viewsPerDay}, Engagement: {engagementRate}";

    private static string BuildGapPrompt(GapAnalysisInput input) =>
        $@"You are an expert YouTube Strategist. Perform a strict two-part analysis for the target video:

        1. ANALYSIS SECTION (Diagnostic Only): Focus purely on 'what is currently happening'. Identify Strengths, Weaknesses, Content Gaps, Audience Pain Points, and Viral Potential based on current state. Do not offer suggestions here.
        2. ROADMAP SECTION (Actionable Strategy Only): Focus purely on 'what to do next'. Provide Missed Opportunities, SEO, CTR, Hook, and Retention improvements. Do not restate the analysis here.

        Return ONLY a raw JSON object (no markdown, no backticks) matching this exact schema: 
        {{ 
          ""analysis"": {{ ""strengths"": [], ""weaknesses"": [], ""contentGaps"": [], ""audiencePainPoints"": [], ""viralPotentialAnalysis"": ""string"" }},
          ""roadmap"": {{ ""missedOpportunities"": [], ""seoRecommendations"": [], ""ctrOptimizationSuggestions"": [], ""hookImprovements"": [], ""retentionImprovements"": [] }},
          ""competitionDifficulty"": 0.0, ""opportunityScore"": 0.0, ""trendGrowth"": 0.0 
        }}
        Target: {input.TargetVideo.Title}
        Competitors: {string.Join("\n", input.CompetitorVideos.Select(v => v.Title))}";

    private sealed class GeminiApiResponseWrapper { public List<GeminiCandidate>? Candidates { get; set; } }
    private sealed class GeminiCandidate { public GeminiContent? Content { get; set; } }
    private sealed class GeminiContent { public List<GeminiPart>? Parts { get; set; } }
    private sealed class GeminiPart { public string? Text { get; set; } }
    
    private sealed class InternalMetricsResponse { public double CompetitionDifficulty { get; set; } public double OpportunityScore { get; set; } public double TrendGrowth { get; set; } public string? Reasoning { get; set; } }
    private sealed class InternalGapAnalysisResponse { 
        public AnalysisSection? Analysis { get; set; }
        public RoadmapSection? Roadmap { get; set; }
        public double CompetitionDifficulty { get; set; } public double OpportunityScore { get; set; } public double TrendGrowth { get; set; }
        
        public List<string>? ContentGaps => Analysis?.ContentGaps;
        public List<string>? AudiencePainPoints => Analysis?.AudiencePainPoints;
        public List<string>? Weaknesses => Analysis?.Weaknesses;
        public List<string>? Strengths => Analysis?.Strengths;
        public string? ViralPotentialAnalysis => Analysis?.ViralPotentialAnalysis;
        public List<string>? MissedOpportunities => Roadmap?.MissedOpportunities;
        public List<string>? SeoRecommendations => Roadmap?.SeoRecommendations;
        public List<string>? CtrOptimizationSuggestions => Roadmap?.CtrOptimizationSuggestions;
        public List<string>? HookImprovements => Roadmap?.HookImprovements;
        public List<string>? RetentionImprovements => Roadmap?.RetentionImprovements;
    }

    private sealed class AnalysisSection {
        public List<string>? Strengths { get; set; }
        public List<string>? Weaknesses { get; set; }
        public List<string>? ContentGaps { get; set; }
        public List<string>? AudiencePainPoints { get; set; }
        public string? ViralPotentialAnalysis { get; set; }
    }

    private sealed class RoadmapSection {
        public List<string>? MissedOpportunities { get; set; }
        public List<string>? SeoRecommendations { get; set; }
        public List<string>? CtrOptimizationSuggestions { get; set; }
        public List<string>? HookImprovements { get; set; }
        public List<string>? RetentionImprovements { get; set; }
    }
}