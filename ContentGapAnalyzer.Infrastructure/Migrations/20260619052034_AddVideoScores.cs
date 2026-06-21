using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContentGapAnalyzer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVideoScores : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "CompetitionScore",
                table: "Videos",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "DemandScore",
                table: "Videos",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "GapScore",
                table: "Videos",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "TrendScore",
                table: "Videos",
                type: "float",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompetitionScore",
                table: "Videos");

            migrationBuilder.DropColumn(
                name: "DemandScore",
                table: "Videos");

            migrationBuilder.DropColumn(
                name: "GapScore",
                table: "Videos");

            migrationBuilder.DropColumn(
                name: "TrendScore",
                table: "Videos");
        }
    }
}
