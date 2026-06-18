using ContentGapAnalyzer.Application.Common;
using ContentGapAnalyzer.Application.DTOs;
using MediatR;

namespace ContentGapAnalyzer.Application.Commands;

public record FetchTrendingVideosCommand(
    string? Region,
    string? CategoryId,
    string? Keywords,
    int MaxResults
) : IRequest<ApiResponse<IReadOnlyList<TrendingVideoDto>>>;

public record AnalyzeGapCommand(
    string VideoId
) : IRequest<ApiResponse<GapReportDto>>;

public record SaveAnalysisSessionCommand(
    int GapReportId,
    string VideoId,
    string Notes,
    string ContextJson
) : IRequest<ApiResponse<AnalysisSessionDto>>;

public record AnalyzeSingleVideoCommand(
    string VideoId
) : IRequest<ApiResponse<GapReportDto>>;