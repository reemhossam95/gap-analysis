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

    private static readonly Dictionary<string, string> CategoryMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "Film", "1" }, { "Autos", "2" }, { "Music", "10" }, { "Pets", "15" },
        { "Sports", "17" }, { "Gaming", "20" }, { "People", "22" }, { "Comedy", "23" },
        { "Entertainment", "24" }, { "News", "25" }, { "Howto", "26" }, { "Education", "27" },
        { "Science", "28" }, { "Travel", "19" }
    };

    private static readonly Dictionary<string, string> CountryMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "Egypt", "EG" }, { "United States", "US" }, { "Saudi Arabia", "SA" },
        { "United Arab Emirates", "AE" }, { "United Kingdom", "GB" }, { "Germany", "DE" },
        { "Canada", "CA" }, { "France", "FR" }, { "Japan", "JP" }, { "India", "IN" }
    };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public YouTubeService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<YouTubeService> logger)
    {
        _httpClient = httpClientFactory.CreateClient("YouTube");
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
        _configuration = configuration;
        _logger = logger;
        _apiKey = configuration["YouTube:ApiKey"]
            ?? throw new InvalidOperationException("YouTube:ApiKey is not configured.");
    }

    public async Task<string?> GetChannelIdByNameAsync(string channelName, CancellationToken cancellationToken = default)
    {
        var url = $"{BaseUrl}/search?part=snippet&type=channel&q={Uri.EscapeDataString(channelName)}&maxResults=1&key={_apiKey}";
        var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        using var document = JsonDocument.Parse(content);
        var root = document.RootElement;

        if (root.TryGetProperty("items", out var items) && items.GetArrayLength() > 0)
        {
            return items[0].GetProperty("snippet").GetProperty("channelId").GetString();
        }
        return null;
    }

    public async Task<IReadOnlyList<TrendingVideoDto>> GetTrendingVideosAsync(
        string region, string categoryId, string keywords, int maxResults,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(keywords))
            {
                return await SearchVideosAsync(keywords, maxResults, cancellationToken);
            }

            var categoryParam = string.IsNullOrWhiteSpace(categoryId) ? "" :
                (CategoryMap.TryGetValue(categoryId, out var mapped) ? mapped : categoryId);

            var regionParam = string.IsNullOrWhiteSpace(region) ? "" :
                (CountryMap.TryGetValue(region, out var countryCode) ? countryCode : region);

            var url = $"{BaseUrl}/videos?part=snippet,statistics,contentDetails" +
                      $"&chart=mostPopular" +
                      (string.IsNullOrWhiteSpace(regionParam) ? "" : $"&regionCode={Uri.EscapeDataString(regionParam)}") +
                      $"&maxResults={maxResults}" +
                      (string.IsNullOrWhiteSpace(categoryParam) ? "" : $"&videoCategoryId={Uri.EscapeDataString(categoryParam)}") +
                      $"&key={_apiKey}";

            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<YouTubeVideoListResponse>(content, JsonOptions);

            return result?.Items is null ? Array.Empty<TrendingVideoDto>() : result.Items.Select(MapToTrendingVideoDto).ToList().AsReadOnly();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while fetching trending videos.");
            throw;
        }
    }

    public async Task<IReadOnlyList<TrendingVideoDto>> GetVideoStatisticsAsync(List<string> videoIds, CancellationToken cancellationToken = default)
    {
        if (videoIds == null || !videoIds.Any()) return Array.Empty<TrendingVideoDto>();
        return await FetchVideoStatsByIdsAsync(string.Join(",", videoIds), cancellationToken);
    }

    public async Task<TrendingVideoDto?> GetVideoDetailsAsync(string videoId, CancellationToken cancellationToken = default)
    {
        var url = $"{BaseUrl}/videos?part=snippet,statistics,contentDetails&id={Uri.EscapeDataString(videoId)}&key={_apiKey}";
        var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        var result = JsonSerializer.Deserialize<YouTubeVideoListResponse>(content, JsonOptions);

        var item = result?.Items?.FirstOrDefault();
        return item is null ? null : MapToTrendingVideoDto(item);
    }

    public async Task<IReadOnlyList<TrendingVideoDto>> SearchVideosAsync(string query, int maxResults, CancellationToken cancellationToken = default)
    {
        var searchUrl = $"{BaseUrl}/search?part=id&q={Uri.EscapeDataString(query)}&type=video&maxResults={maxResults}&order=viewCount&key={_apiKey}";
        var response = await _httpClient.GetAsync(searchUrl, cancellationToken);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        var searchResult = JsonSerializer.Deserialize<YouTubeSearchListResponse>(content, JsonOptions);

        if (searchResult?.Items is null || !searchResult.Items.Any())
            return Array.Empty<TrendingVideoDto>();

        var videoIds = string.Join(",", searchResult.Items.Select(i => i.Id?.VideoId ?? "").Where(id => !string.IsNullOrEmpty(id)));
        return await FetchVideoStatsByIdsAsync(videoIds, cancellationToken);
    }

    public async Task<IReadOnlyList<TrendingVideoDto>> GetVideosByChannelIdAsync(string channelId, int maxResults, CancellationToken cancellationToken = default)
    {
        var searchUrl = $"{BaseUrl}/search?part=id&channelId={Uri.EscapeDataString(channelId)}&type=video&maxResults={maxResults}&order=date&key={_apiKey}";
        var response = await _httpClient.GetAsync(searchUrl, cancellationToken);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        var searchResult = JsonSerializer.Deserialize<YouTubeSearchListResponse>(content, JsonOptions);

        if (searchResult?.Items is null || !searchResult.Items.Any())
            return Array.Empty<TrendingVideoDto>();

        var videoIds = string.Join(",", searchResult.Items.Select(i => i.Id?.VideoId ?? "").Where(id => !string.IsNullOrEmpty(id)));
        return await FetchVideoStatsByIdsAsync(videoIds, cancellationToken);
    }

    private async Task<IReadOnlyList<TrendingVideoDto>> FetchVideoStatsByIdsAsync(string videoIds, CancellationToken cancellationToken)
    {
        var statsUrl = $"{BaseUrl}/videos?part=snippet,statistics,contentDetails&id={videoIds}&key={_apiKey}";
        var response = await _httpClient.GetAsync(statsUrl, cancellationToken);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        var result = JsonSerializer.Deserialize<YouTubeVideoListResponse>(content, JsonOptions);

        return result?.Items is null ? Array.Empty<TrendingVideoDto>() : result.Items.Select(MapToTrendingVideoDto).ToList().AsReadOnly();
    }

    public async Task<IReadOnlyList<TrendingVideoDto>> GetCompetitorVideosAsync(string videoId, int maxResults, CancellationToken cancellationToken = default)
    {
        var targetVideo = await GetVideoDetailsAsync(videoId, cancellationToken);
        if (targetVideo is null) return Array.Empty<TrendingVideoDto>();

        var titleWords = string.Join(" ", targetVideo.Title.Split(' ').Take(5));
        var searchUrl = $"{BaseUrl}/search?part=id&q={Uri.EscapeDataString(titleWords)}&type=video&maxResults={maxResults + 1}&order=relevance&key={_apiKey}";

        var response = await _httpClient.GetAsync(searchUrl, cancellationToken);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        var searchResult = JsonSerializer.Deserialize<YouTubeSearchListResponse>(content, JsonOptions);

        if (searchResult?.Items is null) return Array.Empty<TrendingVideoDto>();

        var competitorIds = string.Join(",", searchResult.Items.Select(i => i.Id?.VideoId ?? "").Where(id => !string.IsNullOrEmpty(id) && id != videoId).Take(maxResults));

        return await FetchVideoStatsByIdsAsync(competitorIds, cancellationToken);
    }

    private static TrendingVideoDto MapToTrendingVideoDto(YouTubeVideoItem item)
    {
        // استخدام null-coalescing لضمان عدم وجود nulls
        var stats = item.Statistics ?? new YouTubeStatistics();
        var snippet = item.Snippet ?? new YouTubeSnippet();
        var thumbnails = snippet.Thumbnails ?? new YouTubeThumbnails();
        var views = ParseLong(stats.ViewCount);
        var likes = ParseLong(stats.LikeCount);
        var comments = ParseLong(stats.CommentCount);

        var daysOld = Math.Max(
            (DateTime.UtcNow - (snippet.PublishedAt ?? DateTime.UtcNow)).TotalDays,
            0.1);

        var velocity = views / Math.Sqrt(daysOld);

        var engagementScore = likes + (comments * 3.0);
        var engagementRate = engagementScore / Math.Max(views, 1.0);

        var gapScore = Math.Round(
            Math.Min((Math.Log10(velocity + 1) * 10) + (engagementRate * 5), 99),
            2);

        var demandScore = Math.Round(
            Math.Max(
                Math.Min((1 - Math.Exp(-(views / 100000.0) * 2)) * 100, 99),
                10),
            2);

        var trendScore = Math.Round(
            Math.Min(velocity / 1000, 100),
            2);

        var competitionScore = Math.Round(
            Math.Min(daysOld * 2 + (100 - (engagementRate * 100)), 95),
            2);
        return new TrendingVideoDto(
            0,
            item.Id ?? string.Empty,
            snippet.Title ?? string.Empty,
            snippet.Description ?? string.Empty,
            snippet.ChannelId ?? string.Empty,
            snippet.ChannelTitle ?? string.Empty,
            thumbnails.Medium?.Url ?? thumbnails.Default?.Url ?? string.Empty,
            ParseLong(stats.ViewCount),
            ParseLong(stats.LikeCount),
            ParseLong(stats.CommentCount),
            snippet.PublishedAt ?? DateTime.UtcNow,
            snippet.CategoryId ?? string.Empty,
            gapScore,
            demandScore,
            competitionScore,
            trendScore
        );
    }

    private static long ParseLong(string? value) => !string.IsNullOrEmpty(value) && long.TryParse(value, out var result) ? result : 0L;

    private sealed class YouTubeVideoListResponse { [JsonPropertyName("items")] public List<YouTubeVideoItem>? Items { get; set; } }
    private sealed class YouTubeSearchListResponse { [JsonPropertyName("items")] public List<YouTubeSearchItem>? Items { get; set; } }
    private sealed class YouTubeVideoItem { [JsonPropertyName("id")] public string? Id { get; set; } [JsonPropertyName("snippet")] public YouTubeSnippet? Snippet { get; set; } [JsonPropertyName("statistics")] public YouTubeStatistics? Statistics { get; set; } }
    private sealed class YouTubeSearchItem { [JsonPropertyName("id")] public YouTubeSearchItemId? Id { get; set; } }
    private sealed class YouTubeSearchItemId { [JsonPropertyName("videoId")] public string? VideoId { get; set; } }
    private sealed class YouTubeSnippet { [JsonPropertyName("title")] public string? Title { get; set; } [JsonPropertyName("description")] public string? Description { get; set; } [JsonPropertyName("channelId")] public string? ChannelId { get; set; } [JsonPropertyName("channelTitle")] public string? ChannelTitle { get; set; } [JsonPropertyName("publishedAt")] public DateTime? PublishedAt { get; set; } [JsonPropertyName("categoryId")] public string? CategoryId { get; set; } [JsonPropertyName("thumbnails")] public YouTubeThumbnails? Thumbnails { get; set; } }
    private sealed class YouTubeThumbnails { [JsonPropertyName("default")] public YouTubeThumbnail? Default { get; set; } [JsonPropertyName("medium")] public YouTubeThumbnail? Medium { get; set; } }
    private sealed class YouTubeThumbnail { [JsonPropertyName("url")] public string? Url { get; set; } }
    private sealed class YouTubeStatistics { [JsonPropertyName("viewCount")] public string? ViewCount { get; set; } [JsonPropertyName("likeCount")] public string? LikeCount { get; set; } [JsonPropertyName("commentCount")] public string? CommentCount { get; set; } }
}