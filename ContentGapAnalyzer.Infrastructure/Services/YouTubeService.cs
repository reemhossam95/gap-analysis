using System.Text.Json;
using System.Text.Json.Serialization;
using ContentGapAnalyzer.Application.DTOs;
using ContentGapAnalyzer.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ContentGapAnalyzer.Infrastructure.Services;

public class YouTubeService : IYouTubeService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<YouTubeService> _logger;
    private readonly string _apiKey;
    private const string BaseUrl = "https://www.googleapis.com/youtube/v3";

    public YouTubeService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<YouTubeService> logger)
    {
        _httpClient = httpClientFactory.CreateClient("YouTube");
        // التعديل: زيادة المهلة الزمنية لـ 30 ثانية لتجنب Timeout
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
        _configuration = configuration;
        _logger = logger;
        _apiKey = configuration["YouTube:ApiKey"]
            ?? throw new InvalidOperationException("YouTube:ApiKey is not configured.");
    }

    public async Task<IReadOnlyList<TrendingVideoDto>> GetTrendingVideosAsync(
        string region, string categoryId, string keywords, int maxResults,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // If keywords provided, use search endpoint; otherwise use videos?chart=mostPopular
            if (!string.IsNullOrWhiteSpace(keywords))
            {
                return await SearchVideosAsync(keywords, maxResults, cancellationToken);
            }

            var url = $"{BaseUrl}/videos?part=snippet,statistics,contentDetails" +
                      $"&chart=mostPopular" +
                      $"&regionCode={Uri.EscapeDataString(region)}" +
                      $"&maxResults={maxResults}" +
                      (string.IsNullOrWhiteSpace(categoryId) ? "" : $"&videoCategoryId={Uri.EscapeDataString(categoryId)}") +
                      $"&key={_apiKey}";

            _logger.LogDebug("Fetching trending videos: {Url}", url.Replace(_apiKey, "***"));

            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<YouTubeVideoListResponse>(content, JsonOptions);

            if (result?.Items is null)
                return Array.Empty<TrendingVideoDto>();

            return result.Items
                .Select(item => MapToTrendingVideoDto(item))
                .ToList()
                .AsReadOnly();
        }
        catch (OperationCanceledException)
        {
            _logger.LogError("Fetching trending videos was cancelled due to timeout.");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while fetching trending videos from YouTube.");
            throw;
        }
    }

    public async Task<TrendingVideoDto?> GetVideoDetailsAsync(
        string videoId, CancellationToken cancellationToken = default)
    {
        var url = $"{BaseUrl}/videos?part=snippet,statistics,contentDetails" +
                  $"&id={Uri.EscapeDataString(videoId)}" +
                  $"&key={_apiKey}";

        _logger.LogDebug("Fetching video details for: {VideoId}", videoId);

        var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        var result = JsonSerializer.Deserialize<YouTubeVideoListResponse>(content, JsonOptions);

        var item = result?.Items?.FirstOrDefault();
        return item is null ? null : MapToTrendingVideoDto(item);
    }

    public async Task<IReadOnlyList<TrendingVideoDto>> SearchVideosAsync(
        string query, int maxResults, CancellationToken cancellationToken = default)
    {
        var searchUrl = $"{BaseUrl}/search?part=snippet" +
                        $"&q={Uri.EscapeDataString(query)}" +
                        $"&type=video" +
                        $"&maxResults={maxResults}" +
                        $"&order=viewCount" +
                        $"&key={_apiKey}";

        _logger.LogDebug("Searching YouTube for: {Query}", query);

        var response = await _httpClient.GetAsync(searchUrl, cancellationToken);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        var searchResult = JsonSerializer.Deserialize<YouTubeSearchListResponse>(content, JsonOptions);

        if (searchResult?.Items is null || !searchResult.Items.Any())
            return Array.Empty<TrendingVideoDto>();

        // Fetch full statistics for search results
        var videoIds = string.Join(",", searchResult.Items.Select(i => i.Id?.VideoId ?? "").Where(id => !string.IsNullOrEmpty(id)));
        if (string.IsNullOrEmpty(videoIds))
            return Array.Empty<TrendingVideoDto>();

        var statsUrl = $"{BaseUrl}/videos?part=snippet,statistics,contentDetails" +
                       $"&id={videoIds}" +
                       $"&key={_apiKey}";

        var statsResponse = await _httpClient.GetAsync(statsUrl, cancellationToken);
        statsResponse.EnsureSuccessStatusCode();

        var statsContent = await statsResponse.Content.ReadAsStringAsync(cancellationToken);
        var statsResult = JsonSerializer.Deserialize<YouTubeVideoListResponse>(statsContent, JsonOptions);

        if (statsResult?.Items is null)
            return Array.Empty<TrendingVideoDto>();

        return statsResult.Items
            .Select(item => MapToTrendingVideoDto(item))
            .ToList()
            .AsReadOnly();
    }

    public async Task<IReadOnlyList<TrendingVideoDto>> GetCompetitorVideosAsync(
        string videoId, int maxResults, CancellationToken cancellationToken = default)
    {
        // Get the target video to extract topic/category
        var targetVideo = await GetVideoDetailsAsync(videoId, cancellationToken);
        if (targetVideo is null)
            return Array.Empty<TrendingVideoDto>();

        // Search for similar videos using the title keywords
        var titleWords = string.Join(" ", targetVideo.Title.Split(' ').Take(5));
        var searchUrl = $"{BaseUrl}/search?part=snippet" +
                        $"&q={Uri.EscapeDataString(titleWords)}" +
                        $"&type=video" +
                        $"&maxResults={maxResults + 1}" +
                        $"&order=relevance" +
                        $"&key={_apiKey}";

        var response = await _httpClient.GetAsync(searchUrl, cancellationToken);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        var searchResult = JsonSerializer.Deserialize<YouTubeSearchListResponse>(content, JsonOptions);

        if (searchResult?.Items is null)
            return Array.Empty<TrendingVideoDto>();

        var competitorIds = searchResult.Items
            .Select(i => i.Id?.VideoId ?? "")
            .Where(id => !string.IsNullOrEmpty(id) && id != videoId)
            .Take(maxResults)
            .ToList();

        if (!competitorIds.Any())
            return Array.Empty<TrendingVideoDto>();

        var statsUrl = $"{BaseUrl}/videos?part=snippet,statistics,contentDetails" +
                       $"&id={string.Join(",", competitorIds)}" +
                       $"&key={_apiKey}";

        var statsResponse = await _httpClient.GetAsync(statsUrl, cancellationToken);
        statsResponse.EnsureSuccessStatusCode();

        var statsContent = await statsResponse.Content.ReadAsStringAsync(cancellationToken);
        var statsResult = JsonSerializer.Deserialize<YouTubeVideoListResponse>(statsContent, JsonOptions);

        if (statsResult?.Items is null)
            return Array.Empty<TrendingVideoDto>();

        return statsResult.Items
            .Select(item => MapToTrendingVideoDto(item))
            .ToList()
            .AsReadOnly();
    }

    private static TrendingVideoDto MapToTrendingVideoDto(YouTubeVideoItem item)
    {
        var stats = item.Statistics;
        var snippet = item.Snippet;

        return new TrendingVideoDto(
            item.Id ?? string.Empty,
            snippet?.Title ?? string.Empty,
            snippet?.ChannelId ?? string.Empty,
            snippet?.ChannelTitle ?? string.Empty,
            snippet?.Thumbnails?.Medium?.Url ?? snippet?.Thumbnails?.Default?.Url ?? string.Empty,
            ParseLong(stats?.ViewCount),
            ParseLong(stats?.LikeCount),
            ParseLong(stats?.CommentCount),
            snippet?.PublishedAt ?? DateTime.UtcNow,
            snippet?.CategoryId ?? string.Empty,
            0, // GapScore
            0, // DemandScore
            0, // CompetitionScore
            0  // TrendScore
        );
    }

    private static long ParseLong(string? value)
        => long.TryParse(value, out var result) ? result : 0L;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };


    private sealed class YouTubeVideoListResponse
    {
        [JsonPropertyName("items")]
        public List<YouTubeVideoItem>? Items { get; set; }
    }

    private sealed class YouTubeSearchListResponse
    {
        [JsonPropertyName("items")]
        public List<YouTubeSearchItem>? Items { get; set; }
    }

    private sealed class YouTubeVideoItem
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("snippet")]
        public YouTubeSnippet? Snippet { get; set; }

        [JsonPropertyName("statistics")]
        public YouTubeStatistics? Statistics { get; set; }
    }

    private sealed class YouTubeSearchItem
    {
        [JsonPropertyName("id")]
        public YouTubeSearchItemId? Id { get; set; }
    }

    private sealed class YouTubeSearchItemId
    {
        [JsonPropertyName("videoId")]
        public string? VideoId { get; set; }
    }

    private sealed class YouTubeSnippet
    {
        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("channelId")]
        public string? ChannelId { get; set; }

        [JsonPropertyName("channelTitle")]
        public string? ChannelTitle { get; set; }

        [JsonPropertyName("publishedAt")]
        public DateTime? PublishedAt { get; set; }

        [JsonPropertyName("categoryId")]
        public string? CategoryId { get; set; }

        [JsonPropertyName("thumbnails")]
        public YouTubeThumbnails? Thumbnails { get; set; }
    }

    private sealed class YouTubeThumbnails
    {
        [JsonPropertyName("default")]
        public YouTubeThumbnail? Default { get; set; }

        [JsonPropertyName("medium")]
        public YouTubeThumbnail? Medium { get; set; }

        [JsonPropertyName("high")]
        public YouTubeThumbnail? High { get; set; }
    }

    private sealed class YouTubeThumbnail
    {
        [JsonPropertyName("url")]
        public string? Url { get; set; }
    }

    private sealed class YouTubeStatistics
    {
        [JsonPropertyName("viewCount")]
        public string? ViewCount { get; set; }

        [JsonPropertyName("likeCount")]
        public string? LikeCount { get; set; }

        [JsonPropertyName("commentCount")]
        public string? CommentCount { get; set; }
    }
}