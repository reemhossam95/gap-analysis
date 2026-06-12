using Asp.Versioning;
using ContentGapAnalyzer.Application.Commands;
using ContentGapAnalyzer.Application.Common;
using ContentGapAnalyzer.Application.DTOs;
using ContentGapAnalyzer.Application.Queries;
using ContentGapAnalyzer.Domain.Entities;
using ContentGapAnalyzer.Domain.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace ContentGapAnalyzer.API.Controllers;

/// <summary>
/// Discovery Engine — fetch trending YouTube videos and perform on-demand analysis.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[EnableRateLimiting("fixed")]
public class DiscoveryController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IUnitOfWork _unitOfWork;

    public DiscoveryController(IMediator mediator, IUnitOfWork unitOfWork) 
    {
        _mediator = mediator;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Get dashboard statistics for the discovery hub.
    /// تم التعديل لتحويل القيم إلى نسب مئوية وتنسيق احترافي.
    /// </summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetDashboardStats(CancellationToken ct)
    {
        var videoRepo = _unitOfWork.Repository<Video>();
        var reportRepo = _unitOfWork.Repository<GapReport>();
        var channelRepo = _unitOfWork.Repository<Channel>();

        // جلب البيانات الخام من قاعدة البيانات
        var totalTopics = await videoRepo.Query().CountAsync(ct);
        var totalReports = await reportRepo.Query().CountAsync(ct);
        var easyWins = await reportRepo.Query().CountAsync(r => r.OpportunityScore > 80, ct);
        var avgScore = await reportRepo.Query().AverageAsync(r => (double?)r.OpportunityScore, ct) ?? 0;
        var highGrowthChannels = await channelRepo.Query().CountAsync(c => c.SubscriberCount > 10000, ct);

        // تنسيق البيانات لتصبح جاهزة للعرض كنسب مئوية أو أرقام منسقة
        var stats = new
        {
            topicsAnalyzed = totalTopics,
            // النسبة المئوية للفوز السهل (Easy Wins) من إجمالي التقارير
            easyWinsPercentage = totalReports > 0 ? Math.Round((double)easyWins / totalReports * 100, 1) : 0,
            // المتوسط كنسبة مئوية (بافتراض أن السكور من 10)
            avgGapPercentage = Math.Round(avgScore * 10, 1),
            highGrowthChannels = highGrowthChannels
        };

        return Ok(new { success = true, data = stats });
    }

    /// <summary>
    /// Fetch trending YouTube videos (Raw metadata only, no AI analysis).
    /// </summary>
    [HttpGet("trending")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<TrendingVideoDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetTrendingVideos(
        [FromQuery] string region = "US",
        [FromQuery] string categoryId = "",
        [FromQuery] string keywords = "",
        [FromQuery] int maxResults = 20,
        CancellationToken cancellationToken = default)
    {
        var command = new FetchTrendingVideosCommand(region, categoryId, keywords, maxResults);
        var result = await _mediator.Send(command, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Get all videos stored in the database for a specific channel.
    /// </summary>
    [HttpGet("channels/{channelId}/videos")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<VideoDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetChannelVideos(
        [FromRoute] string channelId,
        CancellationToken cancellationToken = default)
    {
        var query = new GetVideosByChannelQuery(channelId);
        var result = await _mediator.Send(query, cancellationToken);

        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Analyze a specific video on-demand to get deep content gap insights.
    /// </summary>
    [HttpPost("analyze/{videoId}")]
    [EnableRateLimiting("analysis")]
    [ProducesResponseType(typeof(ApiResponse<GapReportDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> AnalyzeVideo(
        [FromRoute] string videoId,
        CancellationToken cancellationToken = default)
    {
        var command = new AnalyzeGapCommand(videoId);
        var result = await _mediator.Send(command, cancellationToken);

        return result.Success ? Ok(result) : BadRequest(result);
    }
}