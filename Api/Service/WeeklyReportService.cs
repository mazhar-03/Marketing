using Api.Data;
using Api.Data.Entities;
using Api.UI;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SkiaSharp;

namespace Api.Service;

public class WeeklyReportService
{
    private readonly AppDbContext _db;

    // Grafik tasarımlarında kullanılacak kurumsal renk paleti
    private static readonly string[] ChartPalette = new[] 
    { 
        "#1a73e8", "#34a853", "#fbbc04", "#ea4335", "#9334e6", 
        "#00acc1", "#ff6d00", "#e91e63", "#009688", "#795548" 
    };

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

        // Haftanın en yüksek CTR'lı gününü bulma
        var highestCtrDay = dailyTotals
            .OrderByDescending(x => x.CTR)
            .FirstOrDefault();
        
        DateTime? targetHighlightDate = highestCtrDay?.CTR > 0 ? highestCtrDay.Date : null;

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
                            c.Item().Text($"{weekStart:dd MMM yyyy} – {weekEnd.AddDays(-1):dd MMM yyyy}")
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
                            var isHighestCtr = targetHighlightDate.HasValue && day.Date.Date == targetHighlightDate.Value.Date;
                            var bg = isHighestCtr ? "#fff2cc" : (odd ? "#f8f9fa" : "#ffffff");
                            
                            DataCell(table, day.Date.ToString("ddd dd MMM"), bg);
                            DataCell(table, $"${day.Spend:N2}", bg);
                            DataCell(table, $"{day.Clicks:N0}", bg);
                            DataCell(table, $"{day.Impressions:N0}", bg);
                            DataCell(table, $"{day.CTR:N2}%", bg);
                            DataCell(table, $"${day.CPC:N2}", bg);
                            odd = !odd;
                        }
                    });

                    // Hiyerarşik Veri Gruplama
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
                                .Select(adsetGroup => {
                                    var rawAds = adsetGroup.GroupBy(x => x.AdName)
                                        .Select(adGroup => {
                                            var imps = adGroup.Sum(x => x.TotalImpressions);
                                            var clks = adGroup.Sum(x => x.TotalClicks);
                                            return new
                                            {
                                                AdName = adGroup.Key,
                                                Spend = adGroup.Sum(x => x.TotalSpend) * markup,
                                                Clicks = clks,
                                                Impressions = imps,
                                                Conversions = adGroup.Sum(x => x.TotalConversions),
                                                CTR = imps > 0 ? (decimal)clks / imps * 100 : 0
                                            };
                                        })
                                        .OrderByDescending(x => x.Spend)
                                        .ToList();

                                    var topCtrAdName = rawAds.OrderByDescending(a => a.CTR).FirstOrDefault()?.AdName;

                                    return new
                                    {
                                        AdsetName = adsetGroup.Key,
                                        Spend = adsetGroup.Sum(x => x.TotalSpend) * markup,
                                        Clicks = adsetGroup.Sum(x => x.TotalClicks),
                                        Impressions = adsetGroup.Sum(x => x.TotalImpressions),
                                        Conversions = adsetGroup.Sum(x => x.TotalConversions),
                                        TopCtrAdName = topCtrAdName,
                                        Ads = rawAds
                                    };
                                })
                                .OrderByDescending(x => x.Spend)
                                .ToList()
                        })
                        .OrderByDescending(x => x.Spend)
                        .ToList();

                    // ================= 1. ONE PIE CHART FOR CAMPAIGNS OVERALL =================
                    col.Item().PaddingTop(24).Text("Campaign Performance & Visual Analytics").FontSize(14).Bold().FontColor("#1a73e8");
                    
                    var campaignSlices = campaignGroups
                        .Select((c, idx) => (c.CampaignName, c.Spend, ChartPalette[idx % ChartPalette.Length]))
                        .ToList();
                    var campaignChartBytes = GeneratePieChart(campaignSlices);

                    col.Item().PaddingTop(8).Border(1).BorderColor("#e0e0e0").Background("#fafafa").Padding(12).Row(chartRow =>
                    {
                        chartRow.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Overall Campaign Budget Share").FontSize(11).Bold().FontColor("#333333");
                            c.Item().PaddingTop(2).Text("Proportional spend distribution across all active Google Ads campaigns.").FontSize(8.5f).FontColor("#666666");
                            
                            c.Item().PaddingTop(8).Column(legendCol =>
                            {
                                int idx = 0;
                                foreach (var camp in campaignGroups)
                                {
                                    var clr = ChartPalette[idx % ChartPalette.Length];
                                    legendCol.Item().PaddingTop(2).Row(r =>
                                    {
                                        r.ConstantItem(8).AlignMiddle().Height(8).Background(clr);
                                        r.ConstantItem(6);
                                        r.RelativeItem().Text($"{camp.CampaignName}: ${camp.Spend:N2}").FontSize(8.5f).FontColor("#444444");
                                    });
                                    idx++;
                                }
                            });
                        });
                        chartRow.ConstantItem(110).Height(110).Image(campaignChartBytes).FitArea();
                    });

                    // ================= 2. EXCEL PIVOT TABLE WITH INLINE ADSET PIE CHARTS =================
                    col.Item().PaddingTop(16).Table(pivotTable =>
                    {
                        pivotTable.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn(4.5f);
                            cols.RelativeColumn(1.8f);
                            cols.RelativeColumn(1.2f);
                            cols.RelativeColumn(1.5f);
                            cols.RelativeColumn(1f);
                        });

                        pivotTable.Header(h =>
                        {
                            h.Cell().Background("#1a73e8").Padding(6).Text("Marketing Structure").FontSize(9).Bold().FontColor("#ffffff");
                            h.Cell().Background("#1a73e8").Padding(6).Text("Spend").FontSize(9).Bold().FontColor("#ffffff");
                            h.Cell().Background("#1a73e8").Padding(6).Text("Clicks").FontSize(9).Bold().FontColor("#ffffff");
                            h.Cell().Background("#1a73e8").Padding(6).Text("Impressions").FontSize(9).Bold().FontColor("#ffffff");
                            h.Cell().Background("#1a73e8").Padding(6).Text("Conv.").FontSize(9).Bold().FontColor("#ffffff");
                        });

                        foreach (var camp in campaignGroups)
                        {
                            // Kampanya Başlık Satırı
                            var campBg = "#e8f0fe"; 
                            pivotTable.Cell().Background(campBg).BorderBottom(1).BorderColor("#b0c4de").Padding(6).PaddingLeft(6)
                                .Text(camp.CampaignName).FontSize(10).Bold().FontColor("#1a73e8");
                            pivotTable.Cell().Background(campBg).BorderBottom(1).BorderColor("#b0c4de").Padding(6)
                                .Text($"${camp.Spend:N2}").FontSize(10).Bold().FontColor("#1a73e8");
                            pivotTable.Cell().Background(campBg).BorderBottom(1).BorderColor("#b0c4de").Padding(6)
                                .Text(camp.Clicks.ToString("N0")).FontSize(10).Bold().FontColor("#1a73e8");
                            pivotTable.Cell().Background(campBg).BorderBottom(1).BorderColor("#b0c4de").Padding(6)
                                .Text(camp.Impressions.ToString("N0")).FontSize(10).Bold().FontColor("#1a73e8");
                            pivotTable.Cell().Background(campBg).BorderBottom(1).BorderColor("#b0c4de").Padding(6)
                                .Text(camp.Conversions.ToString("N0")).FontSize(10).Bold().FontColor("#1a73e8");

                            // PIE CHART SHOWING EFFICIENCIES BETWEEN ADSETS FOR EACH CAMPAIGN
                            var adsetSlices = camp.Adsets
                                .Select((a, idx) => (a.AdsetName, a.Spend, ChartPalette[idx % ChartPalette.Length]))
                                .ToList();
                            var adsetChartBytes = GeneratePieChart(adsetSlices);

                            pivotTable.Cell().ColumnSpan(5).Background("#ffffff").BorderBottom(0.5f).BorderColor("#e0e0e0").Padding(8).PaddingLeft(18).Row(chartRow =>
                            {
                                chartRow.RelativeItem().Column(c =>
                                {
                                    c.Item().Text("Adset Bütçe Dağılım Payı").FontSize(8.5f).Bold().FontColor("#555555");
                                    int colorIdx = 0;
                                    foreach (var adset in camp.Adsets)
                                    {
                                        var clr = ChartPalette[colorIdx % ChartPalette.Length];
                                        c.Item().PaddingTop(2).Row(r =>
                                        {
                                            r.ConstantItem(6).AlignMiddle().Height(6).Background(clr);
                                            r.ConstantItem(4);
                                            r.RelativeItem().Text($"{adset.AdsetName}: ${adset.Spend:N2}").FontSize(7.5f).FontColor("#666666");
                                        });
                                        colorIdx++;
                                    }
                                });
                                chartRow.ConstantItem(70).Height(70).Image(adsetChartBytes).FitArea();
                            });

                            foreach (var adset in camp.Adsets)
                            {
                                // Adset Satırı
                                var adsetBg = "#f4f9f4";
                                pivotTable.Cell().Background(adsetBg).BorderBottom(0.5f).BorderColor("#d0d0d0").Padding(5).PaddingLeft(18)
                                    .Text($"• {adset.AdsetName}").FontSize(9).Bold().FontColor("#2e7d32");
                                pivotTable.Cell().Background(adsetBg).BorderBottom(0.5f).BorderColor("#d0d0d0").Padding(5)
                                    .Text($"${adset.Spend:N2}").FontSize(9).Bold().FontColor("#2e7d32");
                                pivotTable.Cell().Background(adsetBg).BorderBottom(0.5f).BorderColor("#d0d0d0").Padding(5)
                                    .Text(adset.Clicks.ToString("N0")).FontSize(9).Bold().FontColor("#2e7d32");
                                pivotTable.Cell().Background(adsetBg).BorderBottom(0.5f).BorderColor("#d0d0d0").Padding(5)
                                    .Text(adset.Impressions.ToString("N0")).FontSize(9).Bold().FontColor("#2e7d32");
                                pivotTable.Cell().Background(adsetBg).BorderBottom(0.5f).BorderColor("#d0d0d0").Padding(5)
                                    .Text(adset.Conversions.ToString("N0")).FontSize(9).Bold().FontColor("#2e7d32");

                                foreach (var ad in adset.Ads)
                                {
                                    // CHOOSE HIGHEST CTR AD UNDER EACH ADSET AND COLOR IT YELLOW (#fff2cc)
                                    var isHighestInAdset = adset.TopCtrAdName == ad.AdName && ad.CTR > 0;
                                    
                                    var adBg = isHighestInAdset ? "#fff2cc" : "#ffffff";
                                    var adFore = isHighestInAdset ? "#b8860b" : "#555555";

                                    pivotTable.Cell().Background(adBg).BorderBottom(0.5f).BorderColor("#e0e0e0").Padding(4).PaddingLeft(30)
                                        .Text($"- {ad.AdName}").FontSize(8.5f).FontColor(adFore);
                                    pivotTable.Cell().Background(adBg).BorderBottom(0.5f).BorderColor("#e0e0e0").Padding(4)
                                        .Text($"${ad.Spend:N2}").FontSize(8.5f).FontColor(adFore);
                                    pivotTable.Cell().Background(adBg).BorderBottom(0.5f).BorderColor("#e0e0e0").Padding(4)
                                        .Text(ad.Clicks.ToString("N0")).FontSize(8.5f).FontColor(adFore);
                                    pivotTable.Cell().Background(adBg).BorderBottom(0.5f).BorderColor("#e0e0e0").Padding(4)
                                        .Text(ad.Impressions.ToString("N0")).FontSize(8.5f).FontColor(adFore);
                                    pivotTable.Cell().Background(adBg).BorderBottom(0.5f).BorderColor("#e0e0e0").Padding(4)
                                        .Text(ad.Conversions.ToString("N0")).FontSize(8.5f).FontColor(adFore);
                                }
                            }
                        }
                    });
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

    // ================= SKIASHARP DYNAMIC PIE CHART GENERATOR =================
    private static byte[] GeneratePieChart(List<(string Label, decimal Value, string ColorHex)> slices)
    {
        int width = 240;
        int height = 240;
        using var bitmap = new SKBitmap(width, height);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Transparent);

        decimal total = slices.Sum(s => s.Value);

        if (total == 0)
        {
            using var paint = new SKPaint { Color = SKColors.LightGray, IsAntialias = true, Style = SKPaintStyle.Fill };
            canvas.DrawCircle(width / 2f, height / 2f, width / 2f - 10, paint);
            using var emptyImg = SKImage.FromBitmap(bitmap);
            using var emptyData = emptyImg.Encode(SKEncodedImageFormat.Png, 100);
            return emptyData.ToArray();
        }

        var rect = new SKRect(8, 8, width - 8, height - 8);
        float startAngle = -90f; // Saat 12 yönünden başlaması için

        foreach (var slice in slices)
        {
            if (slice.Value == 0) continue;
            float sweepAngle = (float)(slice.Value / total) * 360f;

            using var paint = new SKPaint
            {
                Color = SKColor.Parse(slice.ColorHex),
                IsAntialias = true,
                Style = SKPaintStyle.Fill
            };

            canvas.DrawArc(rect, startAngle, sweepAngle, true, paint);
            startAngle += sweepAngle;
        }

        using var img = SKImage.FromBitmap(bitmap);
        using var imgData = img.Encode(SKEncodedImageFormat.Png, 100);
        return imgData.ToArray();
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