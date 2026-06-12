using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContentGapAnalyzer.Infrastructure.Migrations
{
    public partial class Baseline : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ⚠️ BASELINE MIGRATION
            // Database already contains schema
            // So we DO NOT recreate tables to avoid conflicts
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Optional: leave empty to avoid dropping existing production data
        }
    }
}