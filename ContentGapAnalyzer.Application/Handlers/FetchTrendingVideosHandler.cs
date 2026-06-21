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
        try
        {
            var region = request.Region ?? "US";
            var categoryId = request.CategoryId ?? string.Empty;
            var keywords = request.Keywords ?? string.Empty;

            bool isChannelSearch = !string.IsNullOrWhiteSpace(keywords) && keywords.StartsWith("@");
            var cacheKey = $"trending:{region}:{categoryId}:{keywords}:{request.MaxResults}";

            if (_cache.TryGetValue(cacheKey, out IReadOnlyList<TrendingVideoDto>? cached) && cached is not null)
            {
                _logger.LogInformation("Cache hit for trending videos key: {CacheKey}", cacheKey);
                return ApiResponse<IReadOnlyList<TrendingVideoDto>>.Ok(cached, "Trending videos retrieved from cache");
            }

            IReadOnlyList<TrendingVideoDto> videos;

            if (isChannelSearch)
            {
                string channelName = keywords.Substring(1);
                var channelId = await _youTubeService.GetChannelIdByNameAsync(channelName, cancellationToken);

                if (string.IsNullOrEmpty(channelId))
                    return ApiResponse<IReadOnlyList<TrendingVideoDto>>.Fail("Channel not found.");

                videos = await _youTubeService.GetVideosByChannelIdAsync(channelId, request.MaxResults, cancellationToken);
            }
            else
            {
                var limit = Math.Min(request.MaxResults, 20);
                _logger.LogInformation("Fetching trending videos from YouTube: region={Region}, limit={Limit}, category={Category}, keywords={Keywords}",
                    region, limit, categoryId, keywords);

                videos = await _youTubeService.GetTrendingVideosAsync(region, categoryId, keywords, limit, cancellationToken);
            }

            var videoRepo = _unitOfWork.Repository<Video>();
            var channelRepo = _unitOfWork.Repository<Channel>();
            var cachedRepo = _unitOfWork.Repository<CachedTrendResult>();
            var gapReportRepo = _unitOfWork.Repository<GapReport>();

            var videoIds = videos.Select(v => v.VideoId).Distinct().ToList();
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

                double daysOld = Math.Max((DateTime.UtcNow - video.PublishedAt).TotalHours / 24.0, 0.1);
                double velocity = video.ViewCount / Math.Pow(daysOld, 0.5);
                double engagementScore = (video.LikeCount * 1.0) + (video.CommentCount * 3.0);
                double engagementRate = engagementScore / Math.Max(video.ViewCount, 1.0);

                double calculatedOpportunity = Math.Round(Math.Min((Math.Log10(velocity + 1) * 10) + (engagementRate * 5), 99), 2);
                calculatedOpportunity = Math.Max(calculatedOpportunity, 35);

                double demandRatio = (double)video.ViewCount / 100000;
                double calculatedDemand = report?.TrendGrowth ?? Math.Round(Math.Max(Math.Min((1 - Math.Exp(-demandRatio * 2)) * 100, 99), 10), 2);

                double calculatedTrend = Math.Round(Math.Min(velocity / 1000, 100), 2);
                double calculatedCompetition = report?.CompetitionDifficulty ?? Math.Round(Math.Min(daysOld * 2 + (100 - (engagementRate * 100)), 95), 2);
                _logger.LogInformation(
    "Video={Title} Views={Views} Likes={Likes} Comments={Comments} Gap={Gap} Demand={Demand} Competition={Competition} Trend={Trend}",
    video.Title,
    video.ViewCount,
    video.LikeCount,
    video.CommentCount,
    calculatedOpportunity,
    calculatedDemand,
    calculatedCompetition,
    calculatedTrend);
                var enriched = new TrendingVideoDto(
                    video.Id,                 // 👈 DB Id
                    video.VideoId,
                    video.Title,
                    video.Description,
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
                ); enrichedVideos.Add(enriched);

                if (!existingChannelIds.Contains(video.ChannelId) && !newChannels.Any(c => c.ChannelId == video.ChannelId))
                {
newChannels.Add(new Channel
{
    ChannelId = video.ChannelId,
    Title = video.ChannelTitle,
    CreatedAt = DateTime.UtcNow
});
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
                        GapScore = report?.OpportunityScore ?? calculatedOpportunity,
                        DemandScore = calculatedDemand,
                        CompetitionScore = Math.Max(calculatedCompetition, 1),
                        TrendScore = report?.TrendGrowth ?? calculatedTrend,

                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });
                }
            }

            foreach (var channel in newChannels) await channelRepo.AddAsync(channel, cancellationToken);
            foreach (var video in newVideos)
            {
                await videoRepo.AddAsync(video, cancellationToken);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken); // 👈 مهم جدًا
var result = newVideos.Select(v => new TrendingVideoDto(
    v.Id,
    v.VideoId,
    v.Title,
    "",
    v.ChannelId,
    "",
    v.ThumbnailUrl,
    v.ViewCount,
    v.LikeCount,
    v.CommentCount,
    v.PublishedAt,
    v.Category,
    v.GapScore,
    v.DemandScore,
    v.CompetitionScore,
    v.TrendScore
)).ToList();
            var cacheRecord = new CachedTrendResult
            {
                CacheKey = cacheKey,
                ResultJson = JsonSerializer.Serialize(enrichedVideos),
                ExpiresAt = DateTime.UtcNow.AddMinutes(15),
                CreatedAt = DateTime.UtcNow
            };
            await cachedRepo.AddAsync(cacheRecord, cancellationToken);
            _cache.Set(cacheKey, enrichedVideos.AsReadOnly(), TimeSpan.FromMinutes(15));


            return ApiResponse<IReadOnlyList<TrendingVideoDto>>.Ok(result.AsReadOnly());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred in FetchTrendingVideosHandler");
            return ApiResponse<IReadOnlyList<TrendingVideoDto>>.Fail("An error occurred while processing your request.");
        }
    }
}