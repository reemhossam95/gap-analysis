using System.Text.Json;
using ContentGapAnalyzer.Application.Commands;
using ContentGapAnalyzer.Application.Common;
using ContentGapAnalyzer.Application.DTOs;
using ContentGapAnalyzer.Application.Interfaces;
using ContentGapAnalyzer.Domain.Entities;
using ContentGapAnalyzer.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace ContentGapAnalyzer.Application.Handlers;

public class FetchTrendingVideosHandler : IRequestHandler<FetchTrendingVideosCommand, ApiResponse<IReadOnlyList<TrendingVideoDto>>>
{
    private readonly IYouTubeService _youTubeService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMemoryCache _cache;
    private readonly ILogger<FetchTrendingVideosHandler> _logger;

    public FetchTrendingVideosHandler(
        IYouTubeService youTubeService,
        IUnitOfWork unitOfWork,
        IMemoryCache cache,
        ILogger<FetchTrendingVideosHandler> logger)
    {
        _youTubeService = youTubeService;
        _unitOfWork = unitOfWork;
        _cache = cache;
        _logger = logger;
    }

    public async Task<ApiResponse<IReadOnlyList<TrendingVideoDto>>> Handle(
        FetchTrendingVideosCommand request,
        CancellationToken cancellationToken)
    {
        var region = request.Region ?? "US";
        var categoryId = request.CategoryId ?? string.Empty;
        var keywords = request.Keywords ?? string.Empty;
        
        var cacheKey = $"trending:{region}:{categoryId}:{keywords}:{request.MaxResults}";

        if (_cache.TryGetValue(cacheKey, out IReadOnlyList<TrendingVideoDto>? cached) && cached is not null)
        {
            _logger.LogInformation("Cache hit for trending videos key: {CacheKey}", cacheKey);
            return ApiResponse<IReadOnlyList<TrendingVideoDto>>.Ok(cached, "Trending videos retrieved from cache");
        }

        var limit = Math.Min(request.MaxResults, 20);
        _logger.LogInformation("Fetching trending videos from YouTube: region={Region}, limit={Limit}, category={Category}, keywords={Keywords}", 
            region, limit, categoryId, keywords);

        var videos = await _youTubeService.GetTrendingVideosAsync(region, categoryId, keywords, limit, cancellationToken);

        var videoRepo = _unitOfWork.Repository<Video>();
        var channelRepo = _unitOfWork.Repository<Channel>();
        var cachedRepo = _unitOfWork.Repository<CachedTrendResult>();
        var gapReportRepo = _unitOfWork.Repository<GapReport>();

        var videoIds = videos.Select(v => v.VideoId).ToList();
        var channelIds = videos.Select(v => v.ChannelId).Distinct().ToList();

        var existingVideos = await videoRepo.FindAsync(v => videoIds.Contains(v.VideoId), cancellationToken);
        var existingVideoIds = existingVideos.Select(v => v.VideoId).ToHashSet();

        var existingChannels = await channelRepo.FindAsync(c => channelIds.Contains(c.ChannelId), cancellationToken);
        var existingChannelIds = existingChannels.Select(c => c.ChannelId).ToHashSet();
        
        var existingReports = await gapReportRepo.FindAsync(r => videoIds.Contains(r.VideoId), cancellationToken);
        var reportDict = existingReports.ToDictionary(r => r.VideoId);

        var enrichedVideos = new List<TrendingVideoDto>();
        var newChannels = new List<Channel>();
        var newVideos = new List<Video>();

        for (int i = 0; i < videos.Count; i++)
        {
            var video = videos[i];
            reportDict.TryGetValue(video.VideoId, out var report);

            // حساب المؤشرات
            double hoursOld = Math.Max((DateTime.UtcNow - video.PublishedAt).TotalHours, 0.1);
            double viewsPerHour = video.ViewCount / hoursOld;
            double engagementRate = (video.LikeCount + (video.CommentCount * 2.0)) / Math.Max(video.ViewCount, 1.0) * 100;
            
            // معادلة Gap Score المحدثة: تعتمد على سرعة الانتشار (Velocity) لضمان أن الفيديوهات الفيرال تأخذ أعلى سكور
            double velocityScore = Math.Log10(viewsPerHour + 1) * 15; 
            double rawScore = velocityScore + (engagementRate * 0.5);
            double calculatedOpportunity = Math.Round(Math.Max(Math.Min(rawScore, 99), 35), 2);
            
            // معادلة Demand: تعتمد على حجم المشاهدات الإجمالي
            double demandRatio = (double)video.ViewCount / 5000000;
            double calculatedDemand = report?.TrendGrowth ?? Math.Round(Math.Max(Math.Min((1 - Math.Exp(-demandRatio * 2)) * 100, 99), 10), 2);
            
            // حساب Trend Score و Competition
            double calculatedTrend = Math.Round(Math.Max(Math.Min(Math.Log10(viewsPerHour + 1) * 20, 100), 10), 2);
            double calculatedCompetition = report?.CompetitionDifficulty ?? Math.Round(Math.Min(hoursOld/24 * 2 + (100 - engagementRate), 95), 2);

            var enriched = new TrendingVideoDto(
                video.VideoId,
                video.Title,
                video.ChannelId,
                video.ChannelTitle,
                video.ThumbnailUrl,
                video.ViewCount,
                video.LikeCount,
                video.CommentCount,
                video.PublishedAt,
                video.Category,
                report?.OpportunityScore ?? calculatedOpportunity,
                calculatedDemand,
                Math.Max(calculatedCompetition, 1),
                report?.TrendGrowth ?? calculatedTrend
            );
            enrichedVideos.Add(enriched);
            
            if (!existingChannelIds.Contains(video.ChannelId) && !newChannels.Any(c => c.ChannelId == video.ChannelId))
            {
                newChannels.Add(new Channel { ChannelId = video.ChannelId, CreatedAt = DateTime.UtcNow });
            }

            if (!existingVideoIds.Contains(video.VideoId) && !newVideos.Any(v => v.VideoId == video.VideoId))
            {
                newVideos.Add(new Video
                {
                    VideoId = video.VideoId,
                    ChannelId = video.ChannelId,
                    Title = video.Title,
                    ThumbnailUrl = video.ThumbnailUrl,
                    ViewCount = video.ViewCount,
                    LikeCount = video.LikeCount,
                    CommentCount = video.CommentCount,
                    PublishedAt = video.PublishedAt,
                    Category = video.Category,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }
        }

        foreach (var channel in newChannels) await channelRepo.AddAsync(channel, cancellationToken);
        foreach (var video in newVideos) await videoRepo.AddAsync(video, cancellationToken);
        
        var cacheRecord = new CachedTrendResult
        {
            CacheKey = cacheKey,
            ResultJson = JsonSerializer.Serialize(enrichedVideos),
            ExpiresAt = DateTime.UtcNow.AddMinutes(15),
            CreatedAt = DateTime.UtcNow
        };
        await cachedRepo.AddAsync(cacheRecord, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        _cache.Set(cacheKey, enrichedVideos.AsReadOnly(), TimeSpan.FromMinutes(15));

        return ApiResponse<IReadOnlyList<TrendingVideoDto>>.Ok(enrichedVideos.AsReadOnly());
    }
}