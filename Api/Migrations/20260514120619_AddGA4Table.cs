using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class AddGA4Table : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GA4DailyInsights",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClientId = table.Column<int>(type: "integer", nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Sessions = table.Column<long>(type: "bigint", nullable: false),
                    TotalUsers = table.Column<long>(type: "bigint", nullable: false),
                    NewUsers = table.Column<long>(type: "bigint", nullable: false),
                    BounceRate = table.Column<decimal>(type: "numeric", nullable: false),
                    AvgSessionDuration = table.Column<decimal>(type: "numeric", nullable: false),
                    PageViews = table.Column<long>(type: "bigint", nullable: false),
                    Source = table.Column<string>(type: "text", nullable: false),
                    Medium = table.Column<string>(type: "text", nullable: false),
                    CampaignName = table.Column<string>(type: "text", nullable: false),
                    Conversions = table.Column<long>(type: "bigint", nullable: false),
                    ConversionEventName = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GA4DailyInsights", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GA4DailyInsights_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GA4DailyInsights_ClientId",
                table: "GA4DailyInsights",
                column: "ClientId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GA4DailyInsights");
        }
    }
}
