using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContentGapAnalyzer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateGapReportWithUserId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "VideoAnalyses",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "GapReports",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "VideoAnalyses");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "GapReports");
        }
    }
}
