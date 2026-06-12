using Asp.Versioning;
using ContentGapAnalyzer.Application.Commands;
using ContentGapAnalyzer.Application.Common;
using ContentGapAnalyzer.Application.DTOs;
using ContentGapAnalyzer.Application.Queries;
using ContentGapAnalyzer.Domain.Entities; // تأكدي من وجودها
using ContentGapAnalyzer.Domain.Interfaces; // تأكدي من وجودها
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore; // ضروري لاستخدام CountAsync و AverageAsync

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
    private readonly IUnitOfWork _unitOfWork; // تمت إضافة الـ IUnitOfWork هنا

    public DiscoveryController(IMediator mediator, IUnitOfWork unitOfWork) 
    {
        _mediator = mediator;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Get dashboard statistics for the discovery hub.
    /// </summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetDashboardStats(CancellationToken ct)
    {
        var videoRepo = _unitOfWork.Repository<Video>();
        var reportRepo = _unitOfWork.Repository<GapReport>();
        var channelRepo = _unitOfWork.Repository<Channel>();

        var stats = new
        {
            topicsAnalyzed = await videoRepo.Query().CountAsync(ct),
            easyWinsFound = await reportRepo.Query().CountAsync(r => r.OpportunityScore > 80, ct),
            avgGapScore = await reportRepo.Query().AverageAsync(r => (double?)r.OpportunityScore, ct) ?? 0,
            highGrowthChannels = await channelRepo.Query().CountAsync(c => c.SubscriberCount > 10000, ct)
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
    /// يستخدم الآن الـ AnalyzeGapCommand الموثوق والمصحح.
    /// </summary>
    [HttpPost("analyze/{videoId}")]
    [EnableRateLimiting("analysis")]
    [ProducesResponseType(typeof(ApiResponse<GapReportDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> AnalyzeVideo(
        [FromRoute] string videoId,
        CancellationToken cancellationToken = default)
    {
        // نقوم باستدعاء الـ Command الصحيح الذي أصلحناه في AnalyzeGapHandler
        var command = new AnalyzeGapCommand(videoId);
        var result = await _mediator.Send(command, cancellationToken);

        return result.Success ? Ok(result) : BadRequest(result);
    }
    }