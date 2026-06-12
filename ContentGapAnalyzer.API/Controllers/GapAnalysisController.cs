using Asp.Versioning;
using ContentGapAnalyzer.Application.Commands;
using ContentGapAnalyzer.Application.Common;
using ContentGapAnalyzer.Application.DTOs;
using ContentGapAnalyzer.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ContentGapAnalyzer.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/gap-analysis")]
[EnableRateLimiting("fixed")]
public class GapAnalysisController : ControllerBase
{
    private readonly IMediator _mediator;

    public GapAnalysisController(IMediator mediator) => _mediator = mediator;

    [HttpPost("analyze")]
    [ProducesResponseType(typeof(ApiResponse<GapReportDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> Analyze(
        [FromBody] AnalyzeRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new AnalyzeGapCommand(request.VideoId);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.Success)
            return result.Message.Contains("not found", StringComparison.OrdinalIgnoreCase)
                ? NotFound(result)
                : BadRequest(result);

        return Ok(result);
    }

    [HttpGet("reports/{videoId}")]
    [ProducesResponseType(typeof(ApiResponse<GapReportDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetReportByVideoId(
        [FromRoute] string videoId,
        CancellationToken cancellationToken = default)
    {
        var query = new GetGapReportByVideoIdQuery(videoId);
        var result = await _mediator.Send(query, cancellationToken);

        return result.Success ? Ok(result) : NotFound(result);
    }
    [HttpGet("history")]
    [ProducesResponseType(typeof(PagedResponse<GapReportDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetHistory(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new GetAnalysisHistoryQuery(page, pageSize);
        var result = await _mediator.Send(query, cancellationToken);

        return result.Success ? Ok(result) : BadRequest(result);
    }
}

public record AnalyzeRequest(string VideoId);
