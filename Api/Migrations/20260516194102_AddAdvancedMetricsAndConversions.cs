using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class AddAdvancedMetricsAndConversions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Spend",
                table: "PlatformDailyInsights",
                newName: "TotalSpend");

            migrationBuilder.RenameColumn(
                name: "Impressions",
                table: "PlatformDailyInsights",
                newName: "TotalViews");

            migrationBuilder.RenameColumn(
                name: "Clicks",
                table: "PlatformDailyInsights",
                newName: "TotalImpressions");

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
                name: "ConversionValue",
                table: "PlatformDailyInsights",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ROAS",
                table: "PlatformDailyInsights",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<long>(
                name: "TotalClicks",
                table: "PlatformDailyInsights",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalConversions",
                table: "PlatformDailyInsights",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "CPA",
                table: "DailyKpis",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "CPV",
                table: "DailyKpis",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Dictionary<string, decimal>>(
                name: "ConversionDetails",
                table: "DailyKpis",
                type: "jsonb",
                nullable: false);

            migrationBuilder.AddColumn<decimal>(
                name: "ConversionValue",
                table: "DailyKpis",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ROAS",
                table: "DailyKpis",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalConversions",
                table: "DailyKpis",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<long>(
                name: "TotalViews",
                table: "DailyKpis",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
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
                name: "ConversionValue",
                table: "PlatformDailyInsights");

            migrationBuilder.DropColumn(
                name: "ROAS",
                table: "PlatformDailyInsights");

            migrationBuilder.DropColumn(
                name: "TotalClicks",
                table: "PlatformDailyInsights");

            migrationBuilder.DropColumn(
                name: "TotalConversions",
                table: "PlatformDailyInsights");

            migrationBuilder.DropColumn(
                name: "CPA",
                table: "DailyKpis");

            migrationBuilder.DropColumn(
                name: "CPV",
                table: "DailyKpis");

            migrationBuilder.DropColumn(
                name: "ConversionDetails",
                table: "DailyKpis");

            migrationBuilder.DropColumn(
                name: "ConversionValue",
                table: "DailyKpis");

            migrationBuilder.DropColumn(
                name: "ROAS",
                table: "DailyKpis");

            migrationBuilder.DropColumn(
                name: "TotalConversions",
                table: "DailyKpis");

            migrationBuilder.DropColumn(
                name: "TotalViews",
                table: "DailyKpis");

            migrationBuilder.RenameColumn(
                name: "TotalViews",
                table: "PlatformDailyInsights",
                newName: "Impressions");

            migrationBuilder.RenameColumn(
                name: "TotalSpend",
                table: "PlatformDailyInsights",
                newName: "Spend");

            migrationBuilder.RenameColumn(
                name: "TotalImpressions",
                table: "PlatformDailyInsights",
                newName: "Clicks");
        }
    }
}
