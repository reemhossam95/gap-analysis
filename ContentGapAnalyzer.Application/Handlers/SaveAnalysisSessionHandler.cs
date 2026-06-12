using ContentGapAnalyzer.Application.Commands;
using ContentGapAnalyzer.Application.Common;
using ContentGapAnalyzer.Application.DTOs;
using ContentGapAnalyzer.Domain.Entities;
using ContentGapAnalyzer.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ContentGapAnalyzer.Application.Handlers;

public class SaveAnalysisSessionHandler : IRequestHandler<SaveAnalysisSessionCommand, ApiResponse<AnalysisSessionDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SaveAnalysisSessionHandler> _logger;

    public SaveAnalysisSessionHandler(IUnitOfWork unitOfWork, ILogger<SaveAnalysisSessionHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ApiResponse<AnalysisSessionDto>> Handle(
        SaveAnalysisSessionCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Saving analysis session for GapReport {GapReportId}", request.GapReportId);

        var gapReportRepo = _unitOfWork.Repository<GapReport>();
        var gapReport = await gapReportRepo.GetByIdAsync(request.GapReportId, cancellationToken);

        if (gapReport is null || gapReport.IsDeleted)
            return ApiResponse<AnalysisSessionDto>.Fail($"Gap report with ID {request.GapReportId} not found.");

        var session = new AnalysisSession
        {
            SessionId = Guid.NewGuid(),
            GapReportId = request.GapReportId,
            VideoId = request.VideoId,
            Notes = request.Notes,
            ContextJson = request.ContextJson,
            SessionDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        var sessionRepo = _unitOfWork.Repository<AnalysisSession>();
        await sessionRepo.AddAsync(session, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Analysis session saved with ID {SessionId}", session.Id);

        var dto = new AnalysisSessionDto(
            session.Id,
            session.SessionId,
            session.GapReportId,
            session.VideoId,
            session.Notes,
            session.SessionDate,
            session.CreatedAt);

        return ApiResponse<AnalysisSessionDto>.Ok(dto, "Analysis session saved successfully");
    }
}
