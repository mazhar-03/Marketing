using Api.Data;
using Api.Data.Entities;
using Api.UI;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Api.Service;

// public class WeeklyReportService
// {
//     private readonly AppDbContext _db;
//
//     public WeeklyReportService(AppDbContext db)
//     {
//         _db = db;
//     }
//
//     public async Task<byte[]> GenerateWeeklyReportAsync(int clientId, DateTime weekStart)
//     {
//         var weekEnd = weekStart.AddDays(7);
//
//         // 1. Fetch KPI data
//         var data = await _db.DailyKpis
//             .Where(x =>
//                 x.ClientId == clientId &&
//                 x.Platform == AdPlatform.GoogleAds &&
//                 x.Date >= weekStart &&
//                 x.Date < weekEnd)
//             .ToListAsync();
//
//         // 2. Aggregate
//         var totalSpend = data.Sum(x => x.TotalSpend);
//         var totalClicks = data.Sum(x => x.TotalClicks);
//         var totalImpressions = data.Sum(x => x.TotalImpressions);
//
//         var ctr = totalImpressions == 0 ? 0 : (decimal)totalClicks / totalImpressions * 100;
//         var cpc = totalClicks == 0 ? 0 : totalSpend / totalClicks;
//         var client = await _db.Clients
//             .FirstAsync(x => x.Id == clientId);
//         
//         // 3. Generate PDF
//         var pdf = Document.Create(container =>
// {
//     container.Page(page =>
//     {
//         page.Size(PageSizes.A4);
//         page.Margin(30);
//
//         // ================= HEADER =================
//         page.Header().Column(header =>
//         {
//             header.Item().Text("Weekly Marketing Performance Report")
//                 .FontSize(24)
//                 .Bold();
//
//             header.Item().Text(client?.Name ?? "Unknown Client")
//                 .FontSize(18)
//                 .SemiBold();
//
//             header.Item().Text(
//                     $"{weekStart:yyyy-MM-dd} → {weekEnd.AddDays(-1):yyyy-MM-dd}")
//                 .FontSize(10)
//                 .FontColor(Colors.Grey.Medium);
//         });
//
//         // ================= CONTENT =================
//         page.Content().PaddingVertical(20).Column(col =>
//         {
//             col.Spacing(20);
//
//             // ================= KPI BOXES =================
//             col.Item().Row(row =>
//             {
//                 row.Spacing(10);
//
//                 row.RelativeItem()
//                     .Component(new KpiBox("Spend", $"{totalSpend:C}"));
//
//                 row.RelativeItem()
//                     .Component(new KpiBox("Clicks", totalClicks.ToString()));
//
//                 row.RelativeItem()
//                     .Component(new KpiBox("Impressions", totalImpressions.ToString()));
//
//                 row.RelativeItem()
//                     .Component(new KpiBox("CTR", $"{ctr:F2}%"));
//             });
//
//             // ================= DAILY BREAKDOWN =================
//             col.Item().Text("Daily Breakdown")
//                 .FontSize(16)
//                 .Bold();
//
//             var dailyBreakdown = data
//                 .GroupBy(x => x.Date.Date)
//                 .Select(g => new
//                 {
//                     Date = g.Key,
//                     Spend = g.Sum(x => x.TotalSpend),
//                     Clicks = g.Sum(x => x.TotalClicks),
//                     Impressions = g.Sum(x => x.TotalImpressions)
//                 })
//                 .OrderBy(x => x.Date)
//                 .ToList();
//
//             col.Item().Table(table =>
//             {
//                 table.ColumnsDefinition(columns =>
//                 {
//                     columns.RelativeColumn();
//                     columns.RelativeColumn();
//                     columns.RelativeColumn();
//                     columns.RelativeColumn();
//                 });
//
//                 // HEADER
//                 table.Header(header =>
//                 {
//                     header.Cell().Padding(5).Background(Colors.Grey.Lighten2)
//                         .Text("Date").Bold();
//
//                     header.Cell().Padding(5).Background(Colors.Grey.Lighten2)
//                         .Text("Spend").Bold();
//
//                     header.Cell().Padding(5).Background(Colors.Grey.Lighten2)
//                         .Text("Clicks").Bold();
//
//                     header.Cell().Padding(5).Background(Colors.Grey.Lighten2)
//                         .Text("Impressions").Bold();
//                 });
//
//                 // ROWS
//                 foreach (var day in dailyBreakdown)
//                 {
//                     table.Cell().Padding(5)
//                         .Text(day.Date.ToString("yyyy-MM-dd"));
//
//                     table.Cell().Padding(5)
//                         .Text($"{day.Spend:0.00} zł");
//
//                     table.Cell().Padding(5)
//                         .Text(day.Clicks.ToString());
//
//                     table.Cell().Padding(5)
//                         .Text(day.Impressions.ToString());
//                 }
//             });
//
//             // ================= CAMPAIGN BREAKDOWN =================
//             col.Item().PaddingTop(10).Text("Campaign Breakdown")
//                 .FontSize(16)
//                 .Bold();
//
//             foreach (var campaignGroup in data.GroupBy(x => x.CampaignName))
//             {
//                 var campaignSpend = campaignGroup.Sum(x => x.TotalSpend);
//                 var campaignClicks = campaignGroup.Sum(x => x.TotalClicks);
//                 var campaignImpressions = campaignGroup.Sum(x => x.TotalImpressions);
//
//                 col.Item().Border(1)
//                     .BorderColor(Colors.Grey.Lighten2)
//                     .Padding(10)
//                     .Column(campaignCol =>
//                     {
//                         campaignCol.Spacing(10);
//
//                         // CAMPAIGN HEADER
//                         campaignCol.Item().Text($"Campaign: {campaignGroup.Key}")
//                             .FontSize(14)
//                             .Bold();
//
//                         campaignCol.Item().Text(
//                             $"Spend: {campaignSpend:C} | Clicks: {campaignClicks} | Impressions: {campaignImpressions}");
//
//                         // ADSETS
//                         foreach (var adsetGroup in campaignGroup.GroupBy(x => x.AdsetName))
//                         {
//                             campaignCol.Item()
//                                 .PaddingLeft(10)
//                                 .Column(adsetCol =>
//                                 {
//                                     adsetCol.Spacing(5);
//
//                                     adsetCol.Item()
//                                         .Text($"Adset: {adsetGroup.Key}")
//                                         .FontSize(12)
//                                         .SemiBold();
//
//                                     // ADS
//                                     foreach (var adGroup in adsetGroup.GroupBy(x => x.AdName))
//                                     {
//                                         var spend = adGroup.Sum(x => x.TotalSpend);
//                                         var clicks = adGroup.Sum(x => x.TotalClicks);
//                                         var impressions = adGroup.Sum(x => x.TotalImpressions);
//
//                                         var adCtr = impressions == 0
//                                             ? 0
//                                             : (decimal)clicks / impressions * 100;
//
//                                         adsetCol.Item()
//                                             .PaddingLeft(15)
//                                             .Text(
//                                                 $"• {adGroup.Key} | Spend: {spend:C} | Clicks: {clicks} | CTR: {adCtr:F2}%");
//                                     }
//                                 });
//                         }
//                     });
//             }
//         });
//
//         // ================= FOOTER =================
//         page.Footer()
//             .AlignCenter()
//             .Text("Generated automatically by Marketing Analytics System")
//             .FontSize(10)
//             .FontColor(Colors.Grey.Medium);
//     });
// })
//         .GeneratePdf();
//
//         return pdf;
//     }
// }

public class WeeklyReportService
{
    private readonly AppDbContext _db;

    public WeeklyReportService(AppDbContext db)
    {
        _db = db;
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<byte[]> GenerateWeeklyReportAsync(int clientId, DateTime weekStart)
    {
        var weekEnd = weekStart.AddDays(6);

        var client = await _db.Clients.FindAsync(clientId)
            ?? throw new KeyNotFoundException($"Client {clientId} not found.");

        // Google Ads KPIs for the week
        var kpis = await _db.DailyKpis
            .Where(x => x.ClientId == clientId
                     && x.Platform == AdPlatform.GoogleAds
                     && x.Date >= weekStart
                     && x.Date <= weekEnd)
            .OrderBy(x => x.Date)
            .ToListAsync();

        // Daily totals for the chart table
        var dailyTotals = kpis
            .GroupBy(x => x.Date)
            .Select(g => new
            {
                Date = g.Key,
                Spend = g.Sum(x => x.TotalSpend),
                Clicks = g.Sum(x => x.TotalClicks),
                Impressions = g.Sum(x => x.TotalImpressions),
                CTR = g.Average(x => x.CTR),
                CPC = g.Average(x => x.CPC),
                CPM = g.Average(x => x.CPM)
            })
            .ToList();

        // Top campaigns by spend
        var topCampaigns = kpis
            .GroupBy(x => x.CampaignName)
            .Select(g => new
            {
                Campaign = g.Key,
                Spend = g.Sum(x => x.TotalSpend),
                Clicks = g.Sum(x => x.TotalClicks),
                Impressions = g.Sum(x => x.TotalImpressions),
                AvgCTR = g.Average(x => x.CTR),
                AvgCPC = g.Average(x => x.CPC)
            })
            .OrderByDescending(x => x.Spend)
            .Take(5)
            .ToList();

        // Weekly totals
        var totalSpend = kpis.Sum(x => x.TotalSpend);
        var totalClicks = kpis.Sum(x => x.TotalClicks);
        var totalImpressions = kpis.Sum(x => x.TotalImpressions);
        var avgCTR = kpis.Any() ? kpis.Average(x => x.CTR) : 0;
        var avgCPC = kpis.Any() ? kpis.Average(x => x.CPC) : 0;
        var avgCPM = kpis.Any() ? kpis.Average(x => x.CPM) : 0;

        // Generate PDF
        var pdf = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Weekly Google Ads Report")
                                .FontSize(20).Bold().FontColor("#1a73e8");
                            c.Item().Text(client.Name)
                                .FontSize(13).FontColor("#444444");
                        });
                        row.ConstantItem(160).AlignRight().Column(c =>
                        {
                            c.Item().Text($"{weekStart:dd MMM yyyy} – {weekEnd:dd MMM yyyy}")
                                .FontSize(10).FontColor("#888888");
                            c.Item().Text($"Generated: {DateTime.UtcNow:dd MMM yyyy HH:mm} UTC")
                                .FontSize(9).FontColor("#aaaaaa");
                        });
                    });

                    col.Item().PaddingTop(8).LineHorizontal(1).LineColor("#1a73e8");
                });

                page.Content().PaddingTop(16).Column(col =>
                {
                    // ── Summary Cards ──
                    col.Item().Text("Weekly Summary").FontSize(13).Bold().FontColor("#333333");
                    col.Item().PaddingTop(8).Row(row =>
                    {
                        SummaryCard(row.RelativeItem(), "Total Spend", $"${totalSpend:N2}", "#1a73e8");
                        row.ConstantItem(8);
                        SummaryCard(row.RelativeItem(), "Total Clicks", $"{totalClicks:N0}", "#34a853");
                        row.ConstantItem(8);
                        SummaryCard(row.RelativeItem(), "Impressions", $"{totalImpressions:N0}", "#fbbc04");
                        row.ConstantItem(8);
                        SummaryCard(row.RelativeItem(), "Avg CTR", $"{avgCTR:N2}%", "#ea4335");
                    });

                    col.Item().PaddingTop(8).Row(row =>
                    {
                        SummaryCard(row.RelativeItem(), "Avg CPC", $"${avgCPC:N2}", "#9334e6");
                        row.ConstantItem(8);
                        SummaryCard(row.RelativeItem(), "Avg CPM", $"${avgCPM:N2}", "#00acc1");
                        row.RelativeItem();
                        row.ConstantItem(8);
                        row.RelativeItem();
                    });

                    // ── Daily Breakdown ──
                    col.Item().PaddingTop(20).Text("Daily Breakdown").FontSize(13).Bold().FontColor("#333333");
                    col.Item().PaddingTop(8).Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn(2);
                            cols.RelativeColumn(2);
                            cols.RelativeColumn(2);
                            cols.RelativeColumn(2);
                            cols.RelativeColumn(2);
                            cols.RelativeColumn(2);
                        });

                        // Header
                        table.Header(header =>
                        {
                            header.Cell().Element(c => TableHeaderCell(c, "Date"));
                            header.Cell().Element(c => TableHeaderCell(c, "Spend"));
                            header.Cell().Element(c => TableHeaderCell(c, "Clicks"));
                            header.Cell().Element(c => TableHeaderCell(c, "Impressions"));
                            header.Cell().Element(c => TableHeaderCell(c, "CTR"));
                            header.Cell().Element(c => TableHeaderCell(c, "CPC"));
                        });

                        // Rows
                        var isOdd = false;
                        foreach (var day in dailyTotals)
                        {
                            var bg = isOdd ? "#f8f9fa" : "#ffffff";
                            TableCell(table, day.Date.ToString("ddd dd MMM"), bg);
                            TableCell(table, $"${day.Spend:N2}", bg);
                            TableCell(table, $"{day.Clicks:N0}", bg);
                            TableCell(table, $"{day.Impressions:N0}", bg);
                            TableCell(table, $"{day.CTR:N2}%", bg);
                            TableCell(table, $"${day.CPC:N2}", bg);
                            isOdd = !isOdd;
                        }
                    });

                    // ── Top Campaigns ──
                    col.Item().PaddingTop(20).Text("Top Campaigns by Spend").FontSize(13).Bold().FontColor("#333333");
                    col.Item().PaddingTop(8).Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn(2);
                            cols.RelativeColumn(2);
                            cols.RelativeColumn(2);
                            cols.RelativeColumn(2);
                            cols.RelativeColumn(2);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(c => TableHeaderCell(c, "Date"));
                            header.Cell().Element(c => TableHeaderCell(c, "Spend"));
                            header.Cell().Element(c => TableHeaderCell(c, "Clicks"));
                            header.Cell().Element(c => TableHeaderCell(c, "Impressions"));
                            header.Cell().Element(c => TableHeaderCell(c, "CTR"));
                            header.Cell().Element(c => TableHeaderCell(c, "CPC"));
                        });

                        var isOdd = false;
                        foreach (var c in topCampaigns)
                        {
                            var bg = isOdd ? "#f8f9fa" : "#ffffff";
                            TableCell(table, c.Campaign, bg);
                            TableCell(table, $"${c.Spend:N2}", bg);
                            TableCell(table, $"{c.Clicks:N0}", bg);
                            TableCell(table, $"{c.AvgCTR:N2}%", bg);
                            TableCell(table, $"${c.AvgCPC:N2}", bg);
                            isOdd = !isOdd;
                        }
                    });
                });

                page.Footer().AlignCenter()
                    .Text(t =>
                    {
                        t.Span("Page ").FontSize(9).FontColor("#aaaaaa");
                        t.CurrentPageNumber().FontSize(9).FontColor("#aaaaaa");
                        t.Span(" of ").FontSize(9).FontColor("#aaaaaa");
                        t.TotalPages().FontSize(9).FontColor("#aaaaaa");
                    });
            });
        });

        return pdf.GeneratePdf();
    }

    private static void SummaryCard(IContainer container, string label, string value, string color)
    {
        container.Border(1).BorderColor("#e0e0e0").Padding(12).Column(col =>
        {
            col.Item().Text(label).FontSize(9).FontColor("#888888");
            col.Item().PaddingTop(4).Text(value).FontSize(16).Bold().FontColor(color);
        });
    }

    private static void TableHeaderCell(IContainer cell, string text)
    {
        cell.Background("#1a73e8")
            .Padding(6)
            .Text(text)
            .FontSize(9)
            .Bold()
            .FontColor("#ffffff");
    }
    private static void TableCell(TableDescriptor table, string text, string bg)
    {
        table.Cell().Background(bg).BorderBottom(1).BorderColor("#e0e0e0").Padding(6)
            .Text(text).FontSize(9).FontColor("#333333");
    }
}