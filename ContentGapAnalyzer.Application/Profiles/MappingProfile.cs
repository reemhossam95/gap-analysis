using System.Text.Json;
using AutoMapper;
using ContentGapAnalyzer.Application.DTOs;
using ContentGapAnalyzer.Domain.Entities;
using ContentGapAnalyzer.Domain.Enums;

namespace ContentGapAnalyzer.Application.Profiles;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Video, VideoDto>().ReverseMap();

        CreateMap<Channel, ChannelDto>().ReverseMap();

        CreateMap<AnalyticsSnapshot, AnalyticsSnapshotDto>().ReverseMap();

        CreateMap<AnalysisSession, AnalysisSessionDto>().ReverseMap();

        CreateMap<GapReport, GapReportDto>()
            .ForMember(dest => dest.ContentGaps,
                opt => opt.MapFrom(src => DeserializeList(src.ContentGapsJson)))
            .ForMember(dest => dest.AudiencePainPoints,
                opt => opt.MapFrom(src => DeserializeList(src.AudiencePainPointsJson)))
            .ForMember(dest => dest.MissedOpportunities,
                opt => opt.MapFrom(src => DeserializeList(src.MissedOpportunitiesJson)))
            .ForMember(dest => dest.Weaknesses,
                opt => opt.MapFrom(src => DeserializeList(src.WeaknessesJson)))
            .ForMember(dest => dest.Strengths,
                opt => opt.MapFrom(src => DeserializeList(src.StrengthsJson)))
            .ForMember(dest => dest.SeoRecommendations,
                opt => opt.MapFrom(src => DeserializeList(src.SeoRecommendationsJson)))
            .ForMember(dest => dest.CtrOptimizationSuggestions,
                opt => opt.MapFrom(src => DeserializeList(src.CtrOptimizationSuggestionsJson)))
            .ForMember(dest => dest.HookImprovements,
                opt => opt.MapFrom(src => DeserializeList(src.HookImprovementsJson)))
            .ForMember(dest => dest.RetentionImprovements,
                opt => opt.MapFrom(src => DeserializeList(src.RetentionImprovementsJson)));

        CreateMap<Opportunity, OpportunityDto>()
            .ForMember(dest => dest.Difficulty,
                opt => opt.MapFrom(src => src.Difficulty.ToString()))
            .ForMember(dest => dest.Tags,
                opt => opt.MapFrom(src => DeserializeList(src.OpportunityTagsJson)));
    }

    private static List<string> DeserializeList(string json)
    {
        try { return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>(); }
        catch { return new List<string>(); }
    }
}
