using System.Globalization;
using Api.Data;
using Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using QuestPDF;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SkiaSharp;

namespace Api.Service;

public class WeeklyReportService
{
    private static readonly string[] ChartPalette = new[]
    {
        "#1a73e8", "#34a853", "#fbbc04", "#ea4335", "#9334e6",
        "#00acc1", "#ff6d00", "#e91e63", "#009688", "#795548"
    };

    private static readonly Dictionary<string, string> MetricLabels = new()
    {
        ["totalSpend"] = "Spend",
        ["totalClicks"] = "Clicks",
        ["totalImpressions"] = "Impressions",
        ["totalViews"] = "Views",
        ["totalConversions"] = "Conversions",
        ["conversionValue"] = "Conv. Value",
        ["ctr"] = "CTR",
        ["cpc"] = "CPC",
        ["cpm"] = "CPM",
        ["cpv"] = "CPV",
        ["cpa"] = "CPA",
        ["roas"] = "ROAS"
    };

    private readonly AppDbContext _db;

    public WeeklyReportService(AppDbContext db)
    {
        _db = db;
        Settings.License = LicenseType.Community;
    }


    public async Task<byte[]> GenerateWeeklyReportAsync(
        int clientId,
        DateTime weekStart,
        decimal markup = 1,
        List<string>? selectedMetrics = null)
    {
        // Binlik ayırıcıyı boşluk, ondalığı virgül yapan özel kuralımız:
        var fmt = new NumberFormatInfo
        {
            NumberGroupSeparator = " ",
            NumberDecimalSeparator = ","
        };

// Kod tekrarını engellemek için mini bir yardımcı fonksiyon:
        string FormatN(decimal v, string f = "N2")
        {
            return v.ToString(f, fmt);
        }

        selectedMetrics ??= new List<string> { "totalSpend", "totalClicks", "totalImpressions", "totalConversions" };
        var metrics = selectedMetrics.Where(m => MetricLabels.ContainsKey(m)).ToList();

        // +7 with < comparison = exactly 7 days including start day
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

        var totalSpend = kpis.Sum(x => x.TotalSpend) * markup;
        var totalClicks = kpis.Sum(x => x.TotalClicks);
        var totalImpressions = kpis.Sum(x => x.TotalImpressions);
        var totalConversions = kpis.Sum(x => x.TotalConversions);
        var avgCTR = totalImpressions > 0 ? (decimal)totalClicks / totalImpressions * 100 : 0;
        var avgCPC = totalClicks > 0 ? totalSpend / totalClicks : 0;
        var avgCPM = totalImpressions > 0 ? totalSpend / totalImpressions * 1000 : 0;

        var dailyTotals = kpis
            .GroupBy(x => x.Date.Date)
            .Select(g => new
            {
                Date = g.Key,
                Spend = g.Sum(x => x.TotalSpend) * markup,
                Clicks = g.Sum(x => x.TotalClicks),
                Impressions = g.Sum(x => x.TotalImpressions),
                Views = g.Sum(x => x.TotalViews),
                Conversions = g.Sum(x => x.TotalConversions),
                ConversionValue = g.Sum(x => x.ConversionValue),
                CTR = g.Sum(x => x.TotalImpressions) > 0
                    ? (decimal)g.Sum(x => x.TotalClicks) / g.Sum(x => x.TotalImpressions) * 100
                    : 0,
                CPC = g.Sum(x => x.TotalClicks) > 0
                    ? g.Sum(x => x.TotalSpend) * markup / g.Sum(x => x.TotalClicks)
                    : 0
            })
            .OrderBy(x => x.Date)
            .ToList();

        var highestCtrDay = dailyTotals.Where(x => x.Impressions > 100).OrderByDescending(x => x.CTR).FirstOrDefault();
        DateTime? targetHighlightDate = highestCtrDay?.CTR > 0 ? highestCtrDay.Date : null;

        string GetDailyMetricValue(string key, dynamic day)
        {
            decimal spend = day.Spend;
            long clicks = day.Clicks;
            long impressions = day.Impressions;
            long views = day.Views;
            decimal conversions = day.Conversions;
            decimal convValue = day.ConversionValue;
            return key switch
            {
                "totalSpend" => $"zł{FormatN(spend)}",
                "totalClicks" => FormatN(clicks, "N0"),
                "totalImpressions" => FormatN(impressions, "N0"),
                "totalViews" => FormatN(views, "N0"),
                "totalConversions" => FormatN(conversions, "N0"),
                "conversionValue" => $"zł{FormatN(convValue)}",
                "ctr" => impressions > 0 ? $"{FormatN((decimal)clicks / impressions * 100)}%" : "0,00%",
                "cpc" => clicks > 0 ? $"zł{FormatN(spend / clicks)}" : "zł0,00",
                "cpm" => impressions > 0 ? $"zł{FormatN(spend / impressions * 1000)}" : "zł0,00",
                "cpv" => views > 0 ? $"zł{FormatN(spend / views)}" : "zł0,00",
                "cpa" => conversions > 0 ? $"zł{FormatN(spend / conversions)}" : "zł0,00",
                "roas" => spend > 0 ? $"{FormatN(convValue / spend)}x" : "0,00x",
                _ => "-"
            };
        }

        string GetCampMetricValue(string key, decimal spend, long clicks, long impressions, long views,
            decimal conversions, decimal convValue)
        {
            return key switch
            {
                "totalSpend" => $"zł{FormatN(spend)}",
                "totalClicks" => FormatN(clicks, "N0"),
                "totalImpressions" => FormatN(impressions, "N0"),
                "totalViews" => FormatN(views, "N0"),
                "totalConversions" => FormatN(conversions, "N0"),
                "conversionValue" => $"zł{FormatN(convValue)}",
                "ctr" => impressions > 0 ? $"{FormatN((decimal)clicks / impressions * 100)}%" : "0,00%",
                "cpc" => clicks > 0 ? $"zł{FormatN(spend / clicks)}" : "zł0,00",
                "cpm" => impressions > 0 ? $"zł{FormatN(spend / impressions * 1000)}" : "zł0,00",
                "cpv" => views > 0 ? $"zł{FormatN(spend / views)}" : "zł0,00",
                "cpa" => conversions > 0 ? $"zł{FormatN(spend / conversions)}" : "zł0,00",
                "roas" => spend > 0 ? $"{FormatN(convValue / spend)}x" : "0,00x",
                _ => "-"
            };
        }

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
                        foreach (var m in metrics)
                        {
                            var (label, value, color) = m switch
                            {
                                "totalSpend" => ("Total spend", $"zł{totalSpend:N2}", "#1a73e8"),
                                "totalClicks" => ("Total clicks", $"{totalClicks:N0}", "#34a853"),
                                "totalImpressions" => ("Impressions", $"{totalImpressions:N0}", "#fbbc04"),
                                "totalConversions" => ("Conversions", $"{totalConversions:N0}", "#ff6d00"),
                                "ctr" => ("Avg CTR", $"{avgCTR:N2}%", "#ea4335"),
                                "cpc" => ("Avg CPC", $"zł{avgCPC:N2}", "#9334e6"),
                                "cpm" => ("Avg CPM", $"zł{avgCPM:N2}", "#00acc1"),
                                _ => (MetricLabels[m], "-", "#999")
                            };

                            row.RelativeItem().PaddingRight(8).Element(c => { SummaryCard(c, label, value, color); });
                        }
                    });

                    // Daily breakdown — dynamic columns
                    col.Item().PaddingTop(20).Text("Daily breakdown").FontSize(13).Bold().FontColor("#333333");
                    col.Item().PaddingTop(8).Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn(2.5f);
                            foreach (var _ in metrics) cols.RelativeColumn(2f);
                        });
                        table.Header(h =>
                        {
                            h.Cell().Element(c => HeaderCell(c, "Date"));
                            foreach (var m in metrics)
                                h.Cell().Element(c => HeaderCell(c, MetricLabels[m]));
                        });
                        var odd = false;
                        foreach (var day in dailyTotals)
                        {
                            var isHighestCtr = targetHighlightDate.HasValue &&
                                               day.Date.Date == targetHighlightDate.Value.Date;
                            var bg = isHighestCtr ? "#fff2cc" : odd ? "#f8f9fa" : "#ffffff";
                            DataCell(table, day.Date.ToString("ddd dd MMM"), bg);
                            foreach (var m in metrics)
                                DataCell(table, GetDailyMetricValue(m, day), bg);
                            odd = !odd;
                        }
                    });

                    // Hierarchical campaign/adset/ad breakdown
                    var campaignGroups = kpis
                        .GroupBy(x => x.CampaignName)
                        .Select(campGroup => new
                        {
                            CampaignName = campGroup.Key,
                            Spend = campGroup.Sum(x => x.TotalSpend) * markup,
                            Clicks = campGroup.Sum(x => x.TotalClicks),
                            Impressions = campGroup.Sum(x => x.TotalImpressions),
                            Views = campGroup.Sum(x => x.TotalViews),
                            Conversions = campGroup.Sum(x => x.TotalConversions),
                            ConversionValue = campGroup.Sum(x => x.ConversionValue),
                            Adsets = campGroup.GroupBy(x => x.AdsetName)
                                .Select(adsetGroup =>
                                {
                                    var rawAds = adsetGroup.GroupBy(x => x.AdName)
                                        .Select(adGroup =>
                                        {
                                            var imps = adGroup.Sum(x => x.TotalImpressions);
                                            var clks = adGroup.Sum(x => x.TotalClicks);
                                            return new
                                            {
                                                AdName = adGroup.Key,
                                                Spend = adGroup.Sum(x => x.TotalSpend) * markup,
                                                Clicks = clks,
                                                Impressions = imps,
                                                Views = adGroup.Sum(x => x.TotalViews),
                                                Conversions = adGroup.Sum(x => x.TotalConversions),
                                                ConversionValue = adGroup.Sum(x => x.ConversionValue),
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
                                        Views = adsetGroup.Sum(x => x.TotalViews),
                                        Conversions = adsetGroup.Sum(x => x.TotalConversions),
                                        ConversionValue = adsetGroup.Sum(x => x.ConversionValue),
                                        TopCtrAdName = topCtrAdName,
                                        Ads = rawAds
                                    };
                                })
                                .OrderByDescending(x => x.Spend)
                                .ToList()
                        })
                        .OrderByDescending(x => x.Spend)
                        .ToList();

                    // Overall campaign pie chart
                    col.Item().PaddingTop(24).Text("Campaign Performance & Visual Analytics").FontSize(14).Bold()
                        .FontColor("#1a73e8");

// Genel toplamı legend'da yüzde göstermek için hesaplıyoruz
                    var overallTotalSpend = campaignGroups.Sum(c => c.Spend);

                    var campaignSlices = campaignGroups
                        .Select((c, idx) => (c.CampaignName, c.Spend, ChartPalette[idx % ChartPalette.Length]))
                        .ToList();
                    var campaignChartBytes = GeneratePieChart(campaignSlices);

// Ana kampanya grafiğini bölmemesi için ShowEntire() ekleyebiliriz (isteğe bağlı ama faydalı)
                    col.Item().ShowEntire().PaddingTop(8).Border(1).BorderColor("#e0e0e0").Background("#fafafa")
                        .Padding(12).Row(
                            chartRow =>
                            {
                                chartRow.RelativeItem().Column(c =>
                                {
                                    c.Item().Text("Overall Campaign Budget Share").FontSize(11).Bold()
                                        .FontColor("#333333");
                                    c.Item().PaddingTop(2)
                                        .Text("Proportional spend distribution across all active Google Ads campaigns.")
                                        .FontSize(8.5f).FontColor("#666666");
                                    c.Item().PaddingTop(8).Column(legendCol =>
                                    {
                                        var idx = 0;
                                        foreach (var camp in campaignGroups)
                                        {
                                            var clr = ChartPalette[idx % ChartPalette.Length];
                                            var campPct = overallTotalSpend > 0
                                                ? camp.Spend / overallTotalSpend * 100
                                                : 0; // Legend için Yüzde

                                            legendCol.Item().PaddingTop(2).Row(r =>
                                            {
                                                r.ConstantItem(8).AlignMiddle().Height(8).Background(clr);
                                                r.ConstantItem(6);
                                                r.RelativeItem()
                                                    .Text(
                                                        $"{camp.CampaignName}: zł{FormatN(camp.Spend)} ({campPct:0.0}%)") // {camp.Spend:N2} yerine FormatN()
                                                    .FontSize(8.5f).FontColor("#444444");
                                            });
                                            idx++;
                                        }
                                    });
                                });
                                chartRow.ConstantItem(110).Height(110).Image(campaignChartBytes).FitArea();
                            });

// Pivot table with dynamic metric columns + inline adset pie charts
                    col.Item().PaddingTop(16).Table(pivotTable =>
                    {
                        pivotTable.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn(4.5f);
                            foreach (var _ in metrics) cols.RelativeColumn(1.8f);
                        });

                        pivotTable.Header(h =>
                        {
                            h.Cell().Background("#1a73e8").Padding(6).Text("Marketing Structure").FontSize(9).Bold().FontColor("#ffffff");
                            foreach (var m in metrics)
                                h.Cell().Background("#1a73e8").Padding(6).Text(MetricLabels[m]).FontSize(9).Bold().FontColor("#ffffff");
                        });

                        bool isFirstCamp = true;

                        foreach (var camp in campaignGroups)
                        {
                            if (!isFirstCamp)
                            {
                                pivotTable.Cell()
                                    .ColumnSpan((uint)(1 + metrics.Count))
                                    .MinHeight(36) // <-- Arayı 18 birim açar
                                    .Background("#ffffff"); // Beyaz arka plan ile temiz bir boşluk yaratır
                            }
                            isFirstCamp = false;

                            var campBg = "#e8f0fe";
                            pivotTable.Cell().Background(campBg).BorderBottom(1).BorderColor("#b0c4de").Padding(6)
                                .PaddingLeft(6)
                                .Text(camp.CampaignName).FontSize(10).Bold().FontColor("#1a73e8");

                            foreach (var m in metrics)
                                pivotTable.Cell().Background(campBg).BorderBottom(1).BorderColor("#b0c4de").Padding(6)
                                    .Text(GetCampMetricValue(m, camp.Spend, camp.Clicks, camp.Impressions, camp.Views,
                                        camp.Conversions, camp.ConversionValue))
                                    .FontSize(10).Bold().FontColor("#1a73e8");

                            var adsetSlices = camp.Adsets
                                .Select((a, idx) => (a.AdsetName, a.Spend, ChartPalette[idx % ChartPalette.Length]))
                                .ToList();
                            var adsetChartBytes = GeneratePieChart(adsetSlices);

                            // DİKKAT: .ShowEntire() ekledik! Bu sayede bu Adset grafiği bulunduğu hücreyle birlikte asla ortadan ikiye sayfa arasına kırılmaz.
                            pivotTable.Cell().ColumnSpan((uint)(1 + metrics.Count))
                                .ShowEntire()
                                .Background("#ffffff").BorderBottom(0.5f).BorderColor("#e0e0e0")
                                .Padding(8).PaddingLeft(18).Row(chartRow =>
                                {
                                    chartRow.RelativeItem().Column(c =>
                                    {
                                        // TÜRKÇE METİN İNGİLİZCEYE ÇEVRİLDİ
                                        c.Item().Text("Adset Budget Distribution Share").FontSize(8.5f).Bold()
                                            .FontColor("#555555");
                                        var colorIdx = 0;

                                        var totalAdsetSpend = camp.Adsets.Sum(a => a.Spend);

                                        foreach (var adset in camp.Adsets)
                                        {
                                            var clr = ChartPalette[colorIdx % ChartPalette.Length];
                                            var adsetPct = totalAdsetSpend > 0
                                                ? adset.Spend / totalAdsetSpend * 100
                                                : 0;

                                            c.Item().PaddingTop(2).Row(r =>
                                            {
                                                r.ConstantItem(6).AlignMiddle().Height(6).Background(clr);
                                                r.ConstantItem(4);
                                                r.RelativeItem()
                                                    .Text(
                                                        $"{adset.AdsetName}: zł{FormatN(adset.Spend)} ({adsetPct:0.0}%)")
                                                    .FontSize(7.5f).FontColor("#666666");
                                            });
                                            colorIdx++;
                                        }
                                    });
                                    chartRow.ConstantItem(70).Height(70).Image(adsetChartBytes).FitArea();
                                });

                            foreach (var adset in camp.Adsets)
                            {
                                var adsetBg = "#f4f9f4";

                                // Satırların bölünmesini engellemek için ShowEntire kullanıyoruz
                                pivotTable.Cell().ShowEntire().Background(adsetBg).BorderBottom(0.5f)
                                    .BorderColor("#d0d0d0")
                                    .Padding(5).PaddingLeft(18)
                                    .Text($"• {adset.AdsetName}").FontSize(9).Bold().FontColor("#2e7d32");

                                foreach (var m in metrics)
                                    pivotTable.Cell().ShowEntire().Background(adsetBg).BorderBottom(0.5f)
                                        .BorderColor("#d0d0d0")
                                        .Padding(5)
                                        .Text(GetCampMetricValue(m, adset.Spend, adset.Clicks, adset.Impressions,
                                            adset.Views, adset.Conversions, adset.ConversionValue))
                                        .FontSize(9).Bold().FontColor("#2e7d32");

                                foreach (var ad in adset.Ads)
                                {
                                    var isHighestInAdset = adset.TopCtrAdName == ad.AdName && ad.CTR > 0;
                                    var adBg = isHighestInAdset ? "#fff2cc" : "#ffffff";
                                    var adFore = isHighestInAdset ? "#b8860b" : "#555555";

                                    pivotTable.Cell().ShowEntire().Background(adBg).BorderBottom(0.5f)
                                        .BorderColor("#e0e0e0")
                                        .Padding(4).PaddingLeft(30)
                                        .Text($"- {ad.AdName}").FontSize(8.5f).FontColor(adFore);

                                    foreach (var m in metrics)
                                        pivotTable.Cell().ShowEntire().Background(adBg).BorderBottom(0.5f)
                                            .BorderColor("#e0e0e0")
                                            .Padding(4)
                                            .Text(GetCampMetricValue(m, ad.Spend, ad.Clicks, ad.Impressions, ad.Views,
                                                ad.Conversions, ad.ConversionValue))
                                            .FontSize(8.5f).FontColor(adFore);
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

    private static byte[] GeneratePieChart(List<(string Label, decimal Value, string ColorHex)> slices)
    {
        int width = 240, height = 240;
        using var bitmap = new SKBitmap(width, height);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Transparent);

        var total = slices.Sum(s => s.Value);
        if (total == 0)
        {
            using var paint = new SKPaint { Color = SKColors.LightGray, IsAntialias = true, Style = SKPaintStyle.Fill };
            canvas.DrawCircle(width / 2f, height / 2f, width / 2f - 10, paint);
            using var emptyImg = SKImage.FromBitmap(bitmap);
            using var emptyData = emptyImg.Encode(SKEncodedImageFormat.Png, 100);
            return emptyData.ToArray();
        }

        var rect = new SKRect(8, 8, width - 8, height - 8);
        var startAngle = -90f;
        var cx = width / 2f;
        var cy = height / 2f;
        var radius = (width - 16) / 2f;

        // Grafik dilimlerinin içine yazılacak yüzde metninin ayarı (Koyu renk ve kalın)
// GeneratePieChart içindeki textPaint ayarını şöyle güncelle:
        using var textPaint = new SKPaint
        {
            Color = SKColor.Parse("#222222"), 
            IsAntialias = true,
            TextSize = 15f, // <-- 13f'den 17f'ye çıkardık, artık çok daha belirgin olacak
            FakeBoldText = true,
            TextAlign = SKTextAlign.Center
        };

        foreach (var slice in slices)
        {
            if (slice.Value == 0) continue;
            var sweepAngle = (float)(slice.Value / total) * 360f;

            // 1. Önce dilimi çiziyoruz
            using var paint = new SKPaint
                { Color = SKColor.Parse(slice.ColorHex), IsAntialias = true, Style = SKPaintStyle.Fill };
            canvas.DrawArc(rect, startAngle, sweepAngle, true, paint);

            // 2. Yüzdeyi hesapla ve grafiğin içine yaz
            var pct = slice.Value / total * 100;

            // Sadece %5'ten büyük dilimlere yazı yazdırıyoruz ki küçük dilimlerde yazılar üst üste binip çirkin durmasın
            if (pct >= 5m)
            {
                var midAngle = startAngle + sweepAngle / 2f;
                var rad = midAngle * Math.PI / 180.0;
                var textRadius = radius * 0.65f; // Metni merkezin %65 uzağına yerleştirir

                var textX = cx + textRadius * (float)Math.Cos(rad);
                var textY = cy + textRadius * (float)Math.Sin(rad) + 5f; // +5 dikey ortalama ayarı

                // Yazıyı yazıyoruz (Eğer bir önceki formatlama fonksiyonunu tanımladıysan {FormatN(pct)} de diyebilirsin)
                canvas.DrawText($"{pct:0.0}%", textX, textY, textPaint);
            }

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