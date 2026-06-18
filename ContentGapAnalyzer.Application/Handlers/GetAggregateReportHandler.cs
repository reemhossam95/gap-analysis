using ContentGapAnalyzer.Application.DTOs;
using ContentGapAnalyzer.Application.Interfaces;
using ContentGapAnalyzer.Application.Queries;
using ContentGapAnalyzer.Domain.Entities;
using ContentGapAnalyzer.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ContentGapAnalyzer.Application.Handlers;

public class GetAggregateReportHandler : IRequestHandler<GetAggregateReportQuery, AggregateReport>
{
    private readonly IGeminiAiService _geminiService;
    private readonly IRepository<GapReport> _repository;

    public GetAggregateReportHandler(IGeminiAiService geminiService, IRepository<GapReport> repository)
    {
        _geminiService = geminiService;
        _repository = repository;
    }

    public async Task<AggregateReport> Handle(GetAggregateReportQuery request, CancellationToken cancellationToken)
    {
        var query = _repository.Query();

        if (!string.IsNullOrEmpty(request.ChannelId))
        {
            query = query.Where(x => x.ChannelId == request.ChannelId);
        }

        var gapReports = await query.ToListAsync(cancellationToken);

        // تحويل البيانات لـ DTO
        var data = gapReports.Select(x => new GapAnalysisResult(
            JsonSerializer.Deserialize<List<string>>(x.ContentGapsJson) ?? new List<string>(),
            JsonSerializer.Deserialize<List<string>>(x.AudiencePainPointsJson) ?? new List<string>(),
            JsonSerializer.Deserialize<List<string>>(x.MissedOpportunitiesJson) ?? new List<string>(),
            JsonSerializer.Deserialize<List<string>>(x.WeaknessesJson) ?? new List<string>(),
            JsonSerializer.Deserialize<List<string>>(x.StrengthsJson) ?? new List<string>(),
            JsonSerializer.Deserialize<List<string>>(x.SeoRecommendationsJson) ?? new List<string>(),
            JsonSerializer.Deserialize<List<string>>(x.CtrOptimizationSuggestionsJson) ?? new List<string>(),
            JsonSerializer.Deserialize<List<string>>(x.HookImprovementsJson) ?? new List<string>(),
            JsonSerializer.Deserialize<List<string>>(x.RetentionImprovementsJson) ?? new List<string>(),
            x.ViralPotentialAnalysis,
            x.CompetitionDifficulty,
            x.OpportunityScore,
            x.TrendGrowth
        )).ToList();

        // هنا التعديل: مطابقة الـ Constructor الجديد للـ AggregateReport
        if (data == null || !data.Any())
        {
            return new AggregateReport(
                new List<string>(), 
                new List<string>(), 
                new List<string>(), 
                new List<string>(), 
                "لا توجد بيانات متاحة لإنشاء خارطة الطريق."
            );
        }

        return await _geminiService.GenerateAggregateReportAsync(data, cancellationToken);
    }
}