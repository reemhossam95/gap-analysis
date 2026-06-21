using ContentGapAnalyzer.Application.DTOs;

namespace ContentGapAnalyzer.Application.Interfaces;

public interface IYouTubeService
{
    Task<string?> GetChannelIdByNameAsync(string channelName, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TrendingVideoDto>> GetTrendingVideosAsync(
        string region, string categoryId, string keywords, int maxResults,
        CancellationToken cancellationToken = default);

    // هذه الميثود هي المفتاح: ستجلب البيانات الكاملة للفيديو (بما فيها المشاهدات واللايكات الحقيقية)
    Task<IReadOnlyList<TrendingVideoDto>> GetVideoStatisticsAsync(
        List<string> videoIds, CancellationToken cancellationToken = default);

    Task<TrendingVideoDto?> GetVideoDetailsAsync(string videoId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TrendingVideoDto>> SearchVideosAsync(
        string query, int maxResults, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TrendingVideoDto>> GetCompetitorVideosAsync(
        string videoId, int maxResults, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TrendingVideoDto>> GetVideosByChannelIdAsync(
        string channelId, int maxResults, CancellationToken cancellationToken = default);
}

public interface IGeminiAiService
{
    Task<VideoMetricsAnalysis> AnalyzeVideoMetricsAsync(
        VideoMetricsInput input, CancellationToken cancellationToken = default);

    Task<GapAnalysisResult> GenerateGapAnalysisAsync(
        GapAnalysisInput input, CancellationToken cancellationToken = default);

    Task<AggregateReport> GenerateAggregateReportAsync(
        List<GapAnalysisResult> previousResults, CancellationToken cancellationToken = default);
}