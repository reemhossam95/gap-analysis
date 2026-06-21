using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContentGapAnalyzer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateListHandling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // تم التعليق على عمليات الـ Drop والـ Rename لأنها تسبب تعارضاً مع الحالة الحالية للقاعدة
            /*
            migrationBuilder.DropForeignKey(
                name: "FK_AnalysisSessions_Content_Gap_Analyses_GapReportId",
                table: "AnalysisSessions");

            migrationBuilder.RenameTable(
                name: "Content_Gap_Analyses",
                newName: "GapReports");
            */
            
            // إذا كانت الجداول موجودة بالفعل بأسماء مختلفة، 
            // سيتم التعامل معها من خلال الـ Model الجديد في الـ AppDbContext
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // لا حاجة لعكس ما لم نقم به
        }
    }
}