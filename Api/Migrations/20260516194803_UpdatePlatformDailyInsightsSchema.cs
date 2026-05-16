using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePlatformDailyInsightsSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CPA",
                table: "PlatformDailyInsights");

            migrationBuilder.DropColumn(
                name: "CPC",
                table: "PlatformDailyInsights");

            migrationBuilder.DropColumn(
                name: "CPM",
                table: "PlatformDailyInsights");

            migrationBuilder.DropColumn(
                name: "CPV",
                table: "PlatformDailyInsights");

            migrationBuilder.DropColumn(
                name: "CTR",
                table: "PlatformDailyInsights");

            migrationBuilder.DropColumn(
                name: "ConversionDetails",
                table: "PlatformDailyInsights");

            migrationBuilder.DropColumn(
                name: "ROAS",
                table: "PlatformDailyInsights");

            migrationBuilder.RenameColumn(
                name: "TotalViews",
                table: "PlatformDailyInsights",
                newName: "Views");

            migrationBuilder.RenameColumn(
                name: "TotalSpend",
                table: "PlatformDailyInsights",
                newName: "Spend");

            migrationBuilder.RenameColumn(
                name: "TotalImpressions",
                table: "PlatformDailyInsights",
                newName: "Impressions");

            migrationBuilder.RenameColumn(
                name: "TotalConversions",
                table: "PlatformDailyInsights",
                newName: "Conversions");

            migrationBuilder.RenameColumn(
                name: "TotalClicks",
                table: "PlatformDailyInsights",
                newName: "Clicks");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Views",
                table: "PlatformDailyInsights",
                newName: "TotalViews");

            migrationBuilder.RenameColumn(
                name: "Spend",
                table: "PlatformDailyInsights",
                newName: "TotalSpend");

            migrationBuilder.RenameColumn(
                name: "Impressions",
                table: "PlatformDailyInsights",
                newName: "TotalImpressions");

            migrationBuilder.RenameColumn(
                name: "Conversions",
                table: "PlatformDailyInsights",
                newName: "TotalConversions");

            migrationBuilder.RenameColumn(
                name: "Clicks",
                table: "PlatformDailyInsights",
                newName: "TotalClicks");

            migrationBuilder.AddColumn<decimal>(
                name: "CPA",
                table: "PlatformDailyInsights",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "CPC",
                table: "PlatformDailyInsights",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "CPM",
                table: "PlatformDailyInsights",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "CPV",
                table: "PlatformDailyInsights",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "CTR",
                table: "PlatformDailyInsights",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Dictionary<string, decimal>>(
                name: "ConversionDetails",
                table: "PlatformDailyInsights",
                type: "jsonb",
                nullable: false);

            migrationBuilder.AddColumn<decimal>(
                name: "ROAS",
                table: "PlatformDailyInsights",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);
        }
    }
}
