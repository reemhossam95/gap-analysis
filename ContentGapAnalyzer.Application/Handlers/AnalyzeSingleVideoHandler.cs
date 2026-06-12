using ContentGapAnalyzer.Application.Commands;
using ContentGapAnalyzer.Application.Common;
using ContentGapAnalyzer.Application.DTOs;
using ContentGapAnalyzer.Application.Interfaces;
using ContentGapAnalyzer.Domain.Entities;
using ContentGapAnalyzer.Domain.Enums;
using ContentGapAnalyzer.Domain.Interfaces;
using MediatR;

namespace ContentGapAnalyzer.Application.Handlers;

public class AnalyzeSingleVideoHandler : IRequestHandler<AnalyzeSingleVideoCommand, ApiResponse<GapReportDto>>
{
    private readonly IGeminiAiService _geminiAiService;
    private readonly IUnitOfWork _unitOfWork;

    public AnalyzeSingleVideoHandler(IGeminiAiService geminiAiService, IUnitOfWork unitOfWork)
    {
        _geminiAiService = geminiAiService;
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<GapReportDto>> Handle(AnalyzeSingleVideoCommand request, CancellationToken cancellationToken)
    {
        var videoRepo = _unitOfWork.Repository<Video>();
        var videos = await videoRepo.FindAsync(v => v.VideoId == request.VideoId, cancellationToken);
        var video = videos.FirstOrDefault();

        if (video == null) 
        {
            return ApiResponse<GapReportDto>.Fail("Video not found in database");
        }

        var analysisRepo = _unitOfWork.Repository<VideoAnalysis>();
        var existingAnalysis = await analysisRepo.FindAsync(a => a.VideoId == request.VideoId, cancellationToken);
        var cached = existingAnalysis.FirstOrDefault();

        if (cached != null)
        {
            return ApiResponse<GapReportDto>.Ok(new GapReportDto(
                cached.Id, video.VideoId, video.Title, video.ChannelId, GapReportStatus.Completed,
                cached.ContentGaps, cached.AudiencePainPoints, cached.MissedOpportunities, 
                cached.Weaknesses, cached.Strengths, cached.SeoRecommendations, 
                cached.CtrOptimizationSuggestions, cached.HookImprovements, cached.RetentionImprovements, 
                cached.ViralPotentialAnalysis, cached.CompetitionDifficulty, cached.OpportunityScore, 
                cached.TrendGrowth, cached.CreatedAt));
        }

        var gapInput = new GapAnalysisInput(
            new VideoBasicInfo(video.VideoId, video.Title),
            new List<VideoBasicInfo>() 
        );

        var analysis = await _geminiAiService.GenerateGapAnalysisAsync(gapInput, cancellationToken);

        var newAnalysis = new VideoAnalysis {
            VideoId = video.VideoId,
            CompetitionDifficulty = analysis.CompetitionDifficulty,
            OpportunityScore = analysis.OpportunityScore,
            TrendGrowth = analysis.TrendGrowth,
            ContentGaps = analysis.ContentGaps ?? new(),
            AudiencePainPoints = analysis.AudiencePainPoints ?? new(),
            MissedOpportunities = analysis.MissedOpportunities ?? new(),
            Weaknesses = analysis.Weaknesses ?? new(),
            Strengths = analysis.Strengths ?? new(),
            SeoRecommendations = analysis.SeoRecommendations ?? new(),
            CtrOptimizationSuggestions = analysis.CtrOptimizationSuggestions ?? new(),
            HookImprovements = analysis.HookImprovements ?? new(),
            RetentionImprovements = analysis.RetentionImprovements ?? new(),
            ViralPotentialAnalysis = analysis.ViralPotentialAnalysis ?? "Summary pending",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        await analysisRepo.AddAsync(newAnalysis, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var report = new GapReportDto(
            newAnalysis.Id,
            video.VideoId,
            video.Title,
            video.ChannelId,
            GapReportStatus.Completed,
            newAnalysis.ContentGaps,
            newAnalysis.AudiencePainPoints,
            newAnalysis.MissedOpportunities,
            newAnalysis.Weaknesses,
            newAnalysis.Strengths,
            newAnalysis.SeoRecommendations,
            newAnalysis.CtrOptimizationSuggestions,
            newAnalysis.HookImprovements,
            newAnalysis.RetentionImprovements,
            newAnalysis.ViralPotentialAnalysis,
            newAnalysis.CompetitionDifficulty,
            newAnalysis.OpportunityScore,
            newAnalysis.TrendGrowth,
            newAnalysis.CreatedAt
        );

        return ApiResponse<GapReportDto>.Ok(report);
    }
}