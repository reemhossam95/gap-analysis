using ContentGapAnalyzer.Application.Common;
using ContentGapAnalyzer.Application.DTOs;
using MediatR;

namespace ContentGapAnalyzer.Application.Queries;

public record GetGapReportByVideoIdQuery(string VideoId) : IRequest<ApiResponse<GapReportDto>>;

public record GetAnalysisHistoryQuery(int Page, int PageSize) : IRequest<PagedResponse<GapReportDto>>;

public record GetAnalysisSessionsByReportQuery(int GapReportId) : IRequest<ApiResponse<IReadOnlyList<AnalysisSessionDto>>>;

public record GetSessionByIdQuery(Guid SessionId) : IRequest<ApiResponse<AnalysisSessionDto>>;

public record GetTrendingVideosFromCacheQuery(
    string Region,
    string CategoryId,
    string Keywords,
    int MaxResults
) : IRequest<ApiResponse<IReadOnlyList<TrendingVideoDto>>>;

// تم التعديل هنا ليصبح ChannelTitle ليتطابق مع اسم الخاصية في الـ Entity
public record GetVideosByChannelQuery(string ChannelTitle) : IRequest<ApiResponse<IReadOnlyList<VideoDto>>>;

// الإضافة الجديدة للتقرير التجميعي:
public record GetAggregateReportQuery(string? ChannelId) : IRequest<AggregateReport>;