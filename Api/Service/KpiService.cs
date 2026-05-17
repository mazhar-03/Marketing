using Api.Data;
using Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Api.Service;

public class KpiService
{
    private readonly AppDbContext _db;

    public KpiService(AppDbContext db)
    {
        _db = db;
    }

    public async Task GenerateDailyKpis(DateTime date)
    {
        var nextDate = date.AddDays(1);

        var rawData = await _db.PlatformDailyInsights
            .Where(x =>
                x.Platform == AdPlatform.GoogleAds &&
                x.Date >= date &&
                x.Date < nextDate)
            .ToListAsync();

        var existing = await _db.DailyKpis
            .Where(x =>
                x.Platform == AdPlatform.GoogleAds &&
                x.Date >= date &&
                x.Date < nextDate)
            .ToListAsync();

        var grouped = rawData.GroupBy(x => new
        {
            x.ClientId,
            x.Platform,
            x.CampaignName,
            x.AdsetName,
            x.AdName
        });

        foreach (var group in grouped)
        {
            var exists = existing.Any(x =>
                x.ClientId == group.Key.ClientId &&
                x.Platform == group.Key.Platform &&
                x.CampaignName == group.Key.CampaignName &&
                x.AdsetName == group.Key.AdsetName &&
                x.AdName == group.Key.AdName);

            if (exists) continue;

            var spend = group.Sum(x => x.Spend);
            var clicks = group.Sum(x => x.Clicks);
            var impressions = group.Sum(x => x.Impressions);
            var views = group.Sum(x => x.Views); // Yeni
            var conversions = group.Sum(x => x.Conversions); // Yeni
            var conversionValue = group.Sum(x => x.ConversionValue); // Yeni

// Güvenli hesaplamalar (Sıfıra bölünme hatasını engelliyoruz)
            var ctr = impressions > 0 ? (decimal)clicks / impressions * 100 : 0;
            var cpc = clicks > 0 ? spend / clicks : 0;
            var cpm = impressions > 0 ? spend / impressions * 1000 : 0;
            var cpv = views > 0 ? spend / views : 0;
            var cpa = conversions > 0 ? spend / conversions : 0;
            var roas = spend > 0 ? conversionValue / spend : 0;

            _db.DailyKpis.Add(new DailyCampaignKPI
            {
                ClientId = group.Key.ClientId,
                Platform = group.Key.Platform,
                Date = date,
                CampaignName = group.Key.CampaignName,
                AdsetName = group.Key.AdsetName,
                AdName = group.Key.AdName,
                TotalSpend = spend,
                TotalClicks = clicks,
                TotalImpressions = impressions,
                TotalViews = views,
                TotalConversions = conversions,
                ConversionValue = conversionValue,

                // Yüzde ve Para birimlerini genelde virgülden sonra 2 hane (Round) tutmak arayüzde rahatlatır
                CTR = Math.Round(ctr, 2),
                CPC = Math.Round(cpc, 2),
                CPM = Math.Round(cpm, 2),
                CPV = Math.Round(cpv, 2),
                CPA = Math.Round(cpa, 2),
                ROAS = Math.Round(roas, 2),

                // EĞER API'den Conversion details çekersen buraya mapleyebilirsin
                ConversionDetails = new Dictionary<string, decimal>() // Fake datada burayı dolduracağız
            });
        }

        await _db.SaveChangesAsync();
    }
}