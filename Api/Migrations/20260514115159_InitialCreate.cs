using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Clients",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    MetaAdAccountId = table.Column<string>(type: "text", nullable: true),
                    MetaAccessToken = table.Column<string>(type: "text", nullable: true),
                    GoogleAdsCustomerId = table.Column<string>(type: "text", nullable: true),
                    GoogleAdsDeveloperToken = table.Column<string>(type: "text", nullable: true),
                    TikTokAdvertiserId = table.Column<string>(type: "text", nullable: true),
                    TikTokAccessToken = table.Column<string>(type: "text", nullable: true),
                    LinkedInAdAccountId = table.Column<string>(type: "text", nullable: true),
                    LinkedInAccessToken = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clients", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DailyKpis",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClientId = table.Column<int>(type: "integer", nullable: false),
                    Platform = table.Column<int>(type: "integer", nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CampaignName = table.Column<string>(type: "text", nullable: false),
                    TotalSpend = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalImpressions = table.Column<long>(type: "bigint", nullable: false),
                    TotalClicks = table.Column<long>(type: "bigint", nullable: false),
                    CTR = table.Column<decimal>(type: "numeric", nullable: false),
                    CPC = table.Column<decimal>(type: "numeric", nullable: false),
                    CPM = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyKpis", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlatformDailyInsights",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClientId = table.Column<int>(type: "integer", nullable: false),
                    Platform = table.Column<int>(type: "integer", nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CampaignName = table.Column<string>(type: "text", nullable: false),
                    AdsetName = table.Column<string>(type: "text", nullable: false),
                    AdName = table.Column<string>(type: "text", nullable: false),
                    Spend = table.Column<decimal>(type: "numeric", nullable: false),
                    Impressions = table.Column<long>(type: "bigint", nullable: false),
                    Clicks = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlatformDailyInsights", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Clients");

            migrationBuilder.DropTable(
                name: "DailyKpis");

            migrationBuilder.DropTable(
                name: "PlatformDailyInsights");
        }
    }
}
