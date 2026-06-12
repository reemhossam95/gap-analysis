using System.Text.Json;
using ContentGapAnalyzer.Application.Common;
using ContentGapAnalyzer.Application.DTOs;
using ContentGapAnalyzer.Application.Queries;
using ContentGapAnalyzer.Domain.Entities;
using ContentGapAnalyzer.Domain.Enums;
using ContentGapAnalyzer.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace ContentGapAnalyzer.Application.Handlers;

public class GetGapReportByVideoIdHandler : IRequestHandler<GetGapReportByVideoIdQuery, ApiResponse<GapReportDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMemoryCache _cache;

    public GetGapReportByVideoIdHandler(IUnitOfWork unitOfWork, IMemoryCache cache)
    {
        _unitOfWork = unitOfWork;
        _cache = cache;
    }

    public async Task<ApiResponse<GapReportDto>> Handle(
        GetGapReportByVideoIdQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = $"gap_report:{request.VideoId}";
        if (_cache.TryGetValue(cacheKey, out GapReportDto? cached) && cached is not null)
            return ApiResponse<GapReportDto>.Ok(cached);

        var repo = _unitOfWork.Repository<GapReport>();
        var reports = await repo.FindAsync(r => r.VideoId == request.VideoId && !r.IsDeleted, cancellationToken);
        var report = reports.OrderByDescending(r => r.CreatedAt).FirstOrDefault();

        if (report is null)
            return ApiResponse<GapReportDto>.Fail($"No gap report found for video ID '{request.VideoId}'.");

        var dto = MapToDto(report);
        _cache.Set(cacheKey, dto, TimeSpan.FromHours(6));
        return ApiResponse<GapReportDto>.Ok(dto);
    }

    internal static GapReportDto MapToDto(GapReport report) => new(
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
        report.CreatedAt);

    private static List<string> Deserialize(string json)
    {
        try { return JsonSerializer.Deserialize<List<string>>(json) ?? new(); }
        catch { return new(); }
    }
}

public class GetAnalysisHistoryHandler : IRequestHandler<GetAnalysisHistoryQuery, PagedResponse<GapReportDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetAnalysisHistoryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    public async Task<PagedResponse<GapReportDto>> Handle(
        GetAnalysisHistoryQuery request, CancellationToken cancellationToken)
    {
        var repo = _unitOfWork.Repository<GapReport>();
        var query = repo.Query()
            .Where(r => !r.IsDeleted)
            .OrderByDescending(r => r.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var dtos = items.Select(GetGapReportByVideoIdHandler.MapToDto).ToList();
        return PagedResponse<GapReportDto>.Ok(dtos, totalCount, request.Page, request.PageSize);
    }
}

public class GetAnalysisSessionsByReportHandler
    : IRequestHandler<GetAnalysisSessionsByReportQuery, ApiResponse<IReadOnlyList<AnalysisSessionDto>>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetAnalysisSessionsByReportHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    public async Task<ApiResponse<IReadOnlyList<AnalysisSessionDto>>> Handle(
        GetAnalysisSessionsByReportQuery request, CancellationToken cancellationToken)
    {
        var repo = _unitOfWork.Repository<AnalysisSession>();
        var sessions = await repo.FindAsync(
            s => s.GapReportId == request.GapReportId && !s.IsDeleted, cancellationToken);

        IReadOnlyList<AnalysisSessionDto> dtos = sessions
            .OrderByDescending(s => s.SessionDate)
            .Select(s => new AnalysisSessionDto(s.Id, s.SessionId, s.GapReportId, s.VideoId, s.Notes, s.SessionDate, s.CreatedAt))
            .ToList()
            .AsReadOnly();

        return ApiResponse<IReadOnlyList<AnalysisSessionDto>>.Ok(dtos);
    }
}

public class GetSessionByIdHandler : IRequestHandler<GetSessionByIdQuery, ApiResponse<AnalysisSessionDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetSessionByIdHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    public async Task<ApiResponse<AnalysisSessionDto>> Handle(
        GetSessionByIdQuery request, CancellationToken cancellationToken)
    {
        var repo = _unitOfWork.Repository<AnalysisSession>();
        var sessions = await repo.FindAsync(s => s.SessionId == request.SessionId && !s.IsDeleted, cancellationToken);
        var session = sessions.FirstOrDefault();

        if (session is null)
            return ApiResponse<AnalysisSessionDto>.Fail($"Session '{request.SessionId}' not found.");

        return ApiResponse<AnalysisSessionDto>.Ok(
            new AnalysisSessionDto(session.Id, session.SessionId, session.GapReportId,
                session.VideoId, session.Notes, session.SessionDate, session.CreatedAt));
    }
}

public class GetVideosByChannelHandler : IRequestHandler<GetVideosByChannelQuery, ApiResponse<IReadOnlyList<VideoDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetVideosByChannelHandler> _logger;

    public GetVideosByChannelHandler(IUnitOfWork unitOfWork, ILogger<GetVideosByChannelHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ApiResponse<IReadOnlyList<VideoDto>>> Handle(
        GetVideosByChannelQuery request, CancellationToken cancellationToken)
    {
        var repo = _unitOfWork.Repository<Video>();
        var videos = await repo.FindAsync(v => v.ChannelId == request.ChannelId && !v.IsDeleted, cancellationToken);

        IReadOnlyList<VideoDto> dtos = videos
            .OrderByDescending(v => v.PublishedAt)
            .Select(v => new VideoDto(v.Id, v.VideoId, v.ChannelId, v.Title, v.Description,
                v.ThumbnailUrl, v.ViewCount, v.LikeCount, v.CommentCount, v.WatchTimeMinutes,
                v.ClickThroughRate, v.AverageViewDuration, v.PublishedAt, v.Category, v.CreatedAt))
            .ToList()
            .AsReadOnly();

        return ApiResponse<IReadOnlyList<VideoDto>>.Ok(dtos);
    }
}
