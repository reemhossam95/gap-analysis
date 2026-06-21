using ContentGapAnalyzer.Application.DTOs;
using ContentGapAnalyzer.Application.Interfaces;
using ContentGapAnalyzer.Application.Queries;
using ContentGapAnalyzer.Domain.Entities;
using ContentGapAnalyzer.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System.Text.Json;
using System.Security.Claims; // تم الإضافة

namespace ContentGapAnalyzer.Application.Handlers;

public class GetAggregateReportHandler : IRequestHandler<GetAggregateReportQuery, AggregateReport>
{
    private readonly IGeminiAiService _geminiService;
    private readonly IRepository<GapReport> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public GetAggregateReportHandler(
        IGeminiAiService geminiService, 
        IRepository<GapReport> repository,
        IUnitOfWork unitOfWork,
        IHttpContextAccessor httpContextAccessor)
    {
        _geminiService = geminiService;
        _repository = repository;
        _unitOfWork = unitOfWork;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<AggregateReport> Handle(GetAggregateReportQuery request, CancellationToken cancellationToken)
    {
        // 1. التحقق من المستخدم وخصم الرصيد
        // استخدام الـ Claims للوصول للـ ID الحقيقي الخاص بالمستخدم
        var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userId))
            throw new UnauthorizedAccessException("User is not authenticated.");
            
        var userRepo = _unitOfWork.Repository<User>();
        
        if (!int.TryParse(userId, out int intUserId))
            throw new Exception("Invalid user identification.");

        var userResult = await userRepo.FindAsync(u => u.Id == intUserId, cancellationToken);
        var user = userResult.FirstOrDefault();

        if (user == null || user.Credits <= 0)
            throw new Exception("Insufficient credits for this aggregate report.");

        // 2. جلب البيانات
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

        // 3. تنفيذ العملية وخصم الرصيد
        var result = await _geminiService.GenerateAggregateReportAsync(data, cancellationToken);

        user.Credits -= 1;
        await userRepo.UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return result;
    }
}