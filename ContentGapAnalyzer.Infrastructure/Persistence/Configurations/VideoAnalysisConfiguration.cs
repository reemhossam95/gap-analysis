using ContentGapAnalyzer.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Text.Json;

namespace ContentGapAnalyzer.Infrastructure.Persistence.Configurations;

public class VideoAnalysisConfiguration : IEntityTypeConfiguration<VideoAnalysis>
{
    public void Configure(EntityTypeBuilder<VideoAnalysis> builder)
    {
        builder.ToTable("VideoAnalyses");
        builder.HasKey(v => v.Id);

        builder.Property(v => v.OpportunityScore).IsRequired();

        // تعريف الخيارات لضمان توافقها مع الـ Serializer
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        // تعريف الـ Converter باستخدام الخيارات
        var listConverter = new ValueConverter<List<string>, string>(
            v => JsonSerializer.Serialize(v, options), 
            v => JsonSerializer.Deserialize<List<string>>(v, options) ?? new List<string>()
        );

        // تطبيق الـ Converter
        builder.Property(v => v.ContentGaps).HasConversion(listConverter);
        builder.Property(v => v.AudiencePainPoints).HasConversion(listConverter);
        builder.Property(v => v.MissedOpportunities).HasConversion(listConverter);
        builder.Property(v => v.Weaknesses).HasConversion(listConverter);
        builder.Property(v => v.Strengths).HasConversion(listConverter);
        builder.Property(v => v.SeoRecommendations).HasConversion(listConverter);
        builder.Property(v => v.CtrOptimizationSuggestions).HasConversion(listConverter);
        builder.Property(v => v.HookImprovements).HasConversion(listConverter);
        builder.Property(v => v.RetentionImprovements).HasConversion(listConverter);
    }
}