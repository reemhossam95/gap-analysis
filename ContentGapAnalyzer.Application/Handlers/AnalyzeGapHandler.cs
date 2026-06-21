using System.Security.Claims;
using System.Text.Json;
using ContentGapAnalyzer.Application.Commands;
using ContentGapAnalyzer.Application.Common;
using ContentGapAnalyzer.Application.DTOs;
using ContentGapAnalyzer.Domain.Entities;
using ContentGapAnalyzer.Domain.Enums;
using ContentGapAnalyzer.Domain.Interfaces;        
using ContentGapAnalyzer.Application.Interfaces;  
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace ContentGapAnalyzer.Application.Handlers;

public class AnalyzeGapHandler : IRequestHandler<AnalyzeGapCommand, ApiResponse<GapReportDto>>
{
    private readonly IYouTubeService _youTubeService;
    private readonly IGeminiAiService _geminiAiService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMemoryCache _cache;
    private readonly ILogger<AnalyzeGapHandler> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AnalyzeGapHandler(
        IYouTubeService youTubeService,
        IGeminiAiService geminiAiService,
        IUnitOfWork unitOfWork,
        IMemoryCache cache,
        ILogger<AnalyzeGapHandler> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _youTubeService = youTubeService;
        _geminiAiService = geminiAiService;
        _unitOfWork = unitOfWork;
        _cache = cache;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<ApiResponse<GapReportDto>> Handle(AnalyzeGapCommand request, CancellationToken cancellationToken)
    {
        // 1. التحقق من هوية المستخدم
        var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return ApiResponse<GapReportDto>.Fail("User is not authenticated.");

        // 2. الوصول لليوزر باستخدام الـ Repository العام
        var userRepo = _unitOfWork.Repository<User>();
        
        if (!int.TryParse(userId, out int intUserId))
            return ApiResponse<GapReportDto>.Fail("Invalid user identification.");

        var userResult = await userRepo.FindAsync(u => u.Id == intUserId, cancellationToken);
        var user = userResult.FirstOrDefault();

        if (user == null)
            return ApiResponse<GapReportDto>.Fail("User not found.");

        var cacheKey = $"gap_report:{request.VideoId}";

        if (_cache.TryGetValue(cacheKey, out GapReportDto? cachedDto) && cachedDto is not null)
        {
            _logger.LogInformation("Cache hit for gap report, videoId: {VideoId}", request.VideoId);
            return ApiResponse<GapReportDto>.Ok(cachedDto, "Gap report retrieved from cache");
        }

        _logger.LogInformation("Starting gap analysis for video: {VideoId}", request.VideoId);

        var gapReportRepo = _unitOfWork.Repository<GapReport>();
        var existing = await gapReportRepo.FindAsync(g => g.VideoId == request.VideoId && !g.IsDeleted && g.UserId == userId, cancellationToken);
        var existingReport = existing.FirstOrDefault();

        if (existingReport?.Status == GapReportStatus.Completed)
        {
            var dto = MapToDto(existingReport);
            _cache.Set(cacheKey, dto, TimeSpan.FromHours(6));
            return ApiResponse<GapReportDto>.Ok(dto, "Gap report retrieved from database");
        }

        var targetVideo = await _youTubeService.GetVideoDetailsAsync(request.VideoId, cancellationToken);
        if (targetVideo is null)
            return ApiResponse<GapReportDto>.Fail($"Video with ID '{request.VideoId}' not found on YouTube");

        _logger.LogInformation("Fetching competitor videos for: {VideoId}", request.VideoId);
        var competitors = await _youTubeService.GetCompetitorVideosAsync(request.VideoId, 10, cancellationToken);

        _logger.LogInformation("Running Gemini AI gap analysis for video: {VideoId}", request.VideoId);
        GapAnalysisResult analysisResult;
        try
        {
            var targetBasic = new VideoBasicInfo(targetVideo.VideoId, targetVideo.Title);
            var compBasics = competitors.Select(c => new VideoBasicInfo(c.VideoId, c.Title)).ToList();
            
            var input = new GapAnalysisInput(targetBasic, compBasics);
            analysisResult = await _geminiAiService.GenerateGapAnalysisAsync(input, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gemini AI gap analysis failed for video {VideoId}", request.VideoId);
            return ApiResponse<GapReportDto>.Fail("AI analysis service is temporarily unavailable. Please try again.");
        }

        var report = existingReport ?? new GapReport
        {
            UserId = userId,
            VideoId = request.VideoId,
            VideoTitle = targetVideo.Title,
            ChannelId = targetVideo.ChannelId,
            CreatedAt = DateTime.UtcNow
        };

        report.Status = GapReportStatus.Completed;
        report.ContentGapsJson = JsonSerializer.Serialize(analysisResult.ContentGaps);
        report.AudiencePainPointsJson = JsonSerializer.Serialize(analysisResult.AudiencePainPoints);
        report.MissedOpportunitiesJson = JsonSerializer.Serialize(analysisResult.MissedOpportunities);
        report.WeaknessesJson = JsonSerializer.Serialize(analysisResult.Weaknesses);
        report.StrengthsJson = JsonSerializer.Serialize(analysisResult.Strengths);
        report.SeoRecommendationsJson = JsonSerializer.Serialize(analysisResult.SeoRecommendations);
        report.CtrOptimizationSuggestionsJson = JsonSerializer.Serialize(analysisResult.CtrOptimizationSuggestions);
        report.HookImprovementsJson = JsonSerializer.Serialize(analysisResult.HookImprovements);
        report.RetentionImprovementsJson = JsonSerializer.Serialize(analysisResult.RetentionImprovements);
        
        report.ViralPotentialAnalysis = analysisResult.ViralPotentialAnalysis;
        report.CompetitionDifficulty = analysisResult.CompetitionDifficulty;
        report.OpportunityScore = analysisResult.OpportunityScore;
        report.TrendGrowth = analysisResult.TrendGrowth;
        report.UpdatedAt = DateTime.UtcNow;
        report.IsDeleted = false;

        if (existingReport is null)
            await gapReportRepo.AddAsync(report, cancellationToken);
        else
            await gapReportRepo.UpdateAsync(report, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var resultDto = MapToDto(report);
        _cache.Set(cacheKey, resultDto, TimeSpan.FromHours(6));

        return ApiResponse<GapReportDto>.Ok(resultDto, "Gap analysis completed successfully");
    }

    private static GapReportDto MapToDto(GapReport report) => new(
        report.Id,
        report.VideoId,
        report.VideoTitle,
        report.ChannelId,
        report.Status,
        Deserialize(report.ContentGapsJson),
        Deserialize(report.AudiencePainPointsJson),
        Deserialize(report.MissedOpportunitiesJson),
        Deserialize(report.WeaknessesJson),
        Deserialize(report.StrengthsJson),
        Deserialize(report.SeoRecommendationsJson),
        Deserialize(report.CtrOptimizationSuggestionsJson),
        Deserialize(report.HookImprovementsJson),
        Deserialize(report.RetentionImprovementsJson),
        report.ViralPotentialAnalysis,
        report.CompetitionDifficulty,
        report.OpportunityScore,
        report.TrendGrowth,
        report.CreatedAt
    );

    private static List<string> Deserialize(string json)
    {
        try { return JsonSerializer.Deserialize<List<string>>(json) ?? new(); }
        catch { return new(); }
    }
}