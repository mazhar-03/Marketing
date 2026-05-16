using Api.Data;
using Api.Data.Entities;
using Api.UI;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Api.Service;

public class WeeklyReportService
{
    private readonly AppDbContext _db;

    public WeeklyReportService(AppDbContext db)
    {
        _db = db;
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<byte[]> GenerateWeeklyReportAsync(int clientId, DateTime weekStart, decimal markup = 1)
    {
        var weekEnd = weekStart.AddDays(7);

        var client = await _db.Clients.FindAsync(clientId)
            ?? throw new KeyNotFoundException($"Client {clientId} not found.");

        var kpis = await _db.DailyKpis
            .Where(x => x.ClientId == clientId
                     && x.Platform == AdPlatform.GoogleAds
                     && x.Date >= weekStart
                     && x.Date < weekEnd)
            .OrderBy(x => x.Date)
            .ToListAsync();

        var totalSpend       = kpis.Sum(x => x.TotalSpend) * markup;
        var totalClicks      = kpis.Sum(x => x.TotalClicks);
        var totalImpressions = kpis.Sum(x => x.TotalImpressions);
        var totalConversions = kpis.Sum(x => x.TotalConversions);
        var avgCTR           = totalImpressions > 0 ? (decimal)totalClicks / totalImpressions * 100 : 0;
        var avgCPC           = totalClicks > 0 ? totalSpend / totalClicks : 0;
        var avgCPM           = totalImpressions > 0 ? totalSpend / totalImpressions * 1000 : 0;

        var dailyTotals = kpis
            .GroupBy(x => x.Date)
            .Select(g => new
            {
                Date        = g.Key,
                Spend       = g.Sum(x => x.TotalSpend) * markup,
                Clicks      = g.Sum(x => x.TotalClicks),
                Impressions = g.Sum(x => x.TotalImpressions),
                CTR         = g.Sum(x => x.TotalImpressions) > 0
                                ? (decimal)g.Sum(x => x.TotalClicks) / g.Sum(x => x.TotalImpressions) * 100 : 0,
                CPC         = g.Sum(x => x.TotalClicks) > 0
                                ? g.Sum(x => x.TotalSpend) * markup / g.Sum(x => x.TotalClicks) : 0,
            })
            .OrderBy(x => x.Date)
            .ToList();

        var pdf = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(10));

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
                        row.ConstantItem(170).AlignRight().Column(c =>
                        {
                            c.Item().Text($"{weekStart:dd MMM yyyy} – {weekEnd:dd MMM yyyy}")
                                .FontSize(10).FontColor("#888888");
                            c.Item().Text($"Generated: {DateTime.UtcNow:dd MMM yyyy HH:mm} UTC")
                                .FontSize(9).FontColor("#aaaaaa");
                            if (markup != 1)
                                c.Item().Text($"Markup: ×{markup:N2}")
                                    .FontSize(9).FontColor("#1a73e8");
                        });
                    });
                    col.Item().PaddingTop(8).LineHorizontal(1).LineColor("#1a73e8");
                });

                page.Content().PaddingTop(16).Column(col =>
                {
                    // Summary cards
                    col.Item().Text("Weekly summary").FontSize(13).Bold().FontColor("#333333");
                    col.Item().PaddingTop(8).Row(row =>
                    {
                        SummaryCard(row.RelativeItem(), "Total spend",  $"${totalSpend:N2}",      "#1a73e8");
                        row.ConstantItem(8);
                        SummaryCard(row.RelativeItem(), "Total clicks", $"{totalClicks:N0}",       "#34a853");
                        row.ConstantItem(8);
                        SummaryCard(row.RelativeItem(), "Impressions",  $"{totalImpressions:N0}",  "#fbbc04");
                        row.ConstantItem(8);
                        SummaryCard(row.RelativeItem(), "Conversions",  $"{totalConversions:N0}",  "#ff6d00");
                    });
                    col.Item().PaddingTop(8).Row(row =>
                    {
                        SummaryCard(row.RelativeItem(), "Avg CTR", $"{avgCTR:N2}%", "#ea4335");
                        row.ConstantItem(8);
                        SummaryCard(row.RelativeItem(), "Avg CPC", $"${avgCPC:N2}", "#9334e6");
                        row.ConstantItem(8);
                        SummaryCard(row.RelativeItem(), "Avg CPM", $"${avgCPM:N2}", "#00acc1");
                        row.ConstantItem(8);
                        row.RelativeItem();
                    });

                    // Daily breakdown
                    col.Item().PaddingTop(20).Text("Daily breakdown").FontSize(13).Bold().FontColor("#333333");
                    col.Item().PaddingTop(8).Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn(2.5f);
                            cols.RelativeColumn(2);
                            cols.RelativeColumn(1.5f);
                            cols.RelativeColumn(2);
                            cols.RelativeColumn(1.5f);
                            cols.RelativeColumn(1.5f);
                        });
                        table.Header(h =>
                        {
                            foreach (var t in new[] { "Date", "Spend", "Clicks", "Impressions", "CTR", "CPC" })
                                h.Cell().Element(c => HeaderCell(c, t));
                        });
                        var odd = false;
                        foreach (var day in dailyTotals)
                        {
                            var bg = odd ? "#f8f9fa" : "#ffffff";
                            DataCell(table, day.Date.ToString("ddd dd MMM"), bg);
                            DataCell(table, $"${day.Spend:N2}", bg);
                            DataCell(table, $"{day.Clicks:N0}", bg);
                            DataCell(table, $"{day.Impressions:N0}", bg);
                            DataCell(table, $"{day.CTR:N2}%", bg);
                            DataCell(table, $"${day.CPC:N2}", bg);
                            odd = !odd;
                        }
                    });

                    // ================= CAMPAIGN, ADSET & ADS BREAKDOWN =================
                    col.Item().PaddingTop(24).Text("Campaign Performance Breakdown").FontSize(14).Bold().FontColor("#1a73e8");
                    col.Item().Text("Detailed breakdown of Campaigns, Adsets, and individual Ads.").FontSize(9).FontColor("#666666");

                    // Group data hierarchically
                    var campaignGroups = kpis
                        .GroupBy(x => x.CampaignName)
                        .Select(campGroup => new
                        {
                            CampaignName = campGroup.Key,
                            Spend = campGroup.Sum(x => x.TotalSpend) * markup,
                            Clicks = campGroup.Sum(x => x.TotalClicks),
                            Impressions = campGroup.Sum(x => x.TotalImpressions),
                            Conversions = campGroup.Sum(x => x.TotalConversions),
                            Adsets = campGroup.GroupBy(x => x.AdsetName)
                                .Select(adsetGroup => new
                                {
                                    AdsetName = adsetGroup.Key,
                                    Spend = adsetGroup.Sum(x => x.TotalSpend) * markup,
                                    Clicks = adsetGroup.Sum(x => x.TotalClicks),
                                    Impressions = adsetGroup.Sum(x => x.TotalImpressions),
                                    Conversions = adsetGroup.Sum(x => x.TotalConversions),
                                    Ads = adsetGroup.GroupBy(x => x.AdName)
                                        .Select(adGroup => new
                                        {
                                            AdName = adGroup.Key,
                                            Spend = adGroup.Sum(x => x.TotalSpend) * markup,
                                            Clicks = adGroup.Sum(x => x.TotalClicks),
                                            Impressions = adGroup.Sum(x => x.TotalImpressions),
                                            Conversions = adGroup.Sum(x => x.TotalConversions)
                                        })
                                        .OrderByDescending(x => x.Spend)
                                        .ToList()
                                })
                                .OrderByDescending(x => x.Spend)
                                .ToList()
                        })
                        .OrderByDescending(x => x.Spend)
                        .ToList();

                    foreach (var camp in campaignGroups)
                    {
                        col.Item().PaddingTop(12).Border(1).BorderColor("#d0d0d0").Background("#fafafa").Padding(10).Column(campCol =>
                        {
                            // 1. CAMPAIGN HEADER BANNER
                            campCol.Item().Row(row =>
                            {
                                row.RelativeItem().Text($"Campaign: {camp.CampaignName}").FontSize(12).Bold().FontColor("#1a73e8");
                                row.ConstantItem(150).AlignRight().Text($"Spend: ${camp.Spend:N2}").FontSize(11).Bold().FontColor("#1a73e8");
                            });
                            
                            campCol.Item().PaddingTop(2).Text($"Clicks: {camp.Clicks:N0} | Impressions: {camp.Impressions:N0} | Conversions: {camp.Conversions:N0}")
                                .FontSize(9).FontColor("#555555");

                            // 2. ADSETS UNDER THIS CAMPAIGN
                            foreach (var adset in camp.Adsets)
                            {
                                campCol.Item().PaddingTop(8).PaddingLeft(12).BorderLeft(2).BorderColor("#34a853").Column(adsetCol =>
                                {
                                    adsetCol.Item().Row(row =>
                                    {
                                        row.RelativeItem().Text($"• Adset: {adset.AdsetName}").FontSize(10).Bold().FontColor("#34a853");
                                        row.ConstantItem(120).AlignRight().Text($"Subtotal: ${adset.Spend:N2}").FontSize(9).Bold().FontColor("#34a853");
                                    });
                                    adsetCol.Item().Text($"  Clicks: {adset.Clicks:N0} | Impressions: {adset.Impressions:N0} | Conversions: {adset.Conversions:N0}")
                                        .FontSize(8.5f).FontColor("#666666");

                                    // 3. ADS UNDER THIS ADSET (Mini Table representation for exact matching view)
                                    adsetCol.Item().PaddingTop(4).PaddingLeft(12).Table(adTable =>
                                    {
                                        adTable.ColumnsDefinition(adCols =>
                                        {
                                            adCols.RelativeColumn(4);
                                            adCols.RelativeColumn(1.5f);
                                            adCols.RelativeColumn(1.2f);
                                            adCols.RelativeColumn(1.5f);
                                            adCols.RelativeColumn(1);
                                        });

                                        adTable.Header(h =>
                                        {
                                            h.Cell().Background("#f0f0f0").Padding(3).Text("Ad Name").FontSize(8).Bold().FontColor("#555555");
                                            h.Cell().Background("#f0f0f0").Padding(3).Text("Spend").FontSize(8).Bold().FontColor("#555555");
                                            h.Cell().Background("#f0f0f0").Padding(3).Text("Clicks").FontSize(8).Bold().FontColor("#555555");
                                            h.Cell().Background("#f0f0f0").Padding(3).Text("Impressions").FontSize(8).Bold().FontColor("#555555");
                                            h.Cell().Background("#f0f0f0").Padding(3).Text("Conv.").FontSize(8).Bold().FontColor("#555555");
                                        });

                                        foreach (var ad in adset.Ads)
                                        {
                                            adTable.Cell().BorderBottom(0.5f).BorderColor("#e0e0e0").Padding(3).Text(ad.AdName).FontSize(8).FontColor("#444444");
                                            adTable.Cell().BorderBottom(0.5f).BorderColor("#e0e0e0").Padding(3).Text($"${ad.Spend:N2}").FontSize(8).FontColor("#444444");
                                            adTable.Cell().BorderBottom(0.5f).BorderColor("#e0e0e0").Padding(3).Text(ad.Clicks.ToString()).FontSize(8).FontColor("#444444");
                                            adTable.Cell().BorderBottom(0.5f).BorderColor("#e0e0e0").Padding(3).Text(ad.Impressions.ToString()).FontSize(8).FontColor("#444444");
                                            adTable.Cell().BorderBottom(0.5f).BorderColor("#e0e0e0").Padding(3).Text(ad.Conversions.ToString()).FontSize(8).FontColor("#444444");
                                        }
                                    });
                                });
                            }
                        });
                    }
                });

                page.Footer().AlignCenter().Text(t =>
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
            col.Item().PaddingTop(4).Text(value).FontSize(15).Bold().FontColor(color);
        });
    }

    private static void HeaderCell(IContainer cell, string text)
    {
        cell.Background("#1a73e8").Padding(6)
            .Text(text).FontSize(9).Bold().FontColor("#ffffff");
    }

    private static void DataCell(TableDescriptor table, string text, string bg)
    {
        table.Cell().Background(bg).BorderBottom(1).BorderColor("#e0e0e0").Padding(6)
            .Text(text).FontSize(9).FontColor("#333333");
    }
}