using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContentGapAnalyzer.Infrastructure.Migrations
{
    public partial class AddVideoAnalysisTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VideoAnalyses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),

                    VideoId = table.Column<string>(
                        type: "nvarchar(max)",
                        nullable: false),

                    CompetitionDifficulty = table.Column<double>(
                        type: "float",
                        nullable: false),

                    OpportunityScore = table.Column<double>(
                        type: "float",
                        nullable: false),

                    TrendGrowth = table.Column<double>(
                        type: "float",
                        nullable: false),

                    CreatedAt = table.Column<DateTime>(
                        type: "datetime2",
                        nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VideoAnalyses", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VideoAnalyses");
        }
    }
}