using ContentGapAnalyzer.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContentGapAnalyzer.Infrastructure.Persistence.Configurations;

public class GapReportConfiguration : IEntityTypeConfiguration<GapReport>
{
    public void Configure(EntityTypeBuilder<GapReport> builder)
    {
        // التعديل هنا: غيرنا اسم الجدول من Content_Gap_Analyses إلى GapReports
        // لأن هو ده الجدول اللي فيه الأعمدة اللي الكود بيدور عليها
        builder.ToTable("GapReports");
        
        builder.HasKey(e => e.Id);

        builder.Property(e => e.OpportunityScore).IsRequired();
        
        // إذا استمر الخطأ في أعمدة أخرى، يجب عليكِ التأكد من أنها موجودة في جدول GapReports
        // باستخدام الـ Query اللي جربناه قبل قليل.
    }
}