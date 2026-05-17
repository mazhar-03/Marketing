using Api.Data;
using Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Api.Service;

public class FakeDataService
{
    private readonly AppDbContext _db;
    private readonly Random _random = new();

    public FakeDataService(AppDbContext db)
    {
        _db = db;
    }

    public async Task SeedAsync()
    {
        var clients = await _db.Clients.ToListAsync();

        // Sadece 3 Kampanya
        var campaigns = new[]
        {
            "Brand Awareness",
            "Summer Sale",
            "Retargeting"
        };

        // Her kampanyada 3 Adset
        var adsets = new[]
        {
            "Cold Audience",
            "Warm Audience",
            "Remarketing"
        };

        // Her adsette 3 Ad
        var ads = new[]
        {
            "Video Ad A",
            "Image Ad B",
            "Carousel Ad C"
        };

        var startDate = DateTime.UtcNow.AddDays(-14);

        foreach (var client in clients)
            for (var day = 0; day < 14; day++)
            {
                var date = startDate.AddDays(day);

                foreach (var campaign in campaigns)
                foreach (var adset in adsets)
                foreach (var ad in ads)
                {
                    // === 1. HAM VERİLERİN ÜRETİLMESİ ===
                    var spend = Math.Round((decimal)(_random.NextDouble() * 200 + 10), 2);
                    var impressions = _random.Next(500, 20000);
                    if (impressions == 0) continue;

                    var clicks = _random.Next(1, (int)(impressions * 0.1) + 2);
                    var isVideo = ad.Contains("Video") || ad.Contains("Reels");
                    var views = isVideo ? _random.Next(clicks, impressions) : 0;
                    var totalConversions = Math.Round((decimal)(_random.NextDouble() * (clicks * 0.15)), 2);
                    var conversionValue = Math.Round(totalConversions * (decimal)(_random.NextDouble() * 130 + 20), 2);

                    // === 2. HAM VERİ TABLOSUNA EKLEME (PlatformDailyInsights) ===
                    _db.PlatformDailyInsights.Add(new PlatformDailyInsight
                    {
                        ClientId = client.Id,
                        Platform = AdPlatform.GoogleAds,
                        Date = date,
                        CampaignName = campaign,
                        AdsetName = adset,
                        AdName = ad,
                        Spend = spend,
                        Impressions = impressions,
                        Clicks = clicks,
                        Views = views,
                        Conversions = totalConversions,
                        ConversionValue = conversionValue
                    });

                    // === 3. ORANLARIN HESAPLANMASI ===
                    var ctr = impressions > 0 ? (decimal)clicks / impressions * 100 : 0;
                    var cpc = clicks > 0 ? spend / clicks : 0;
                    var cpm = impressions > 0 ? spend / impressions * 1000 : 0;
                    var cpv = views > 0 ? spend / views : 0;
                    var cpa = totalConversions > 0 ? spend / totalConversions : 0;
                    var roas = spend > 0 ? conversionValue / spend : 0;

                    var conversionDetails = new Dictionary<string, decimal>();
                    if (totalConversions > 0)
                    {
                        var weightPurchase = (decimal)_random.NextDouble();
                        var weightLead = (decimal)_random.NextDouble();
                        var totalWeight = weightPurchase + weightLead;

                        conversionDetails.Add("Purchase",
                            Math.Round(totalConversions * (weightPurchase / totalWeight), 2));
                        conversionDetails.Add("Lead", Math.Round(totalConversions * (weightLead / totalWeight), 2));
                    }

                    // === 4. RAPOR TABLOSUNA EKLEME (DailyKpis) ===
                    _db.DailyKpis.Add(new DailyCampaignKPI
                    {
                        ClientId = client.Id,
                        Platform = AdPlatform.GoogleAds,
                        Date = date,
                        CampaignName = campaign,
                        AdsetName = adset,
                        AdName = ad,
                        TotalSpend = spend,
                        TotalClicks = clicks,
                        TotalImpressions = impressions,
                        TotalViews = views,
                        TotalConversions = totalConversions,
                        ConversionValue = conversionValue,
                        ConversionDetails = conversionDetails,
                        CTR = Math.Round(ctr, 2),
                        CPC = Math.Round(cpc, 2),
                        CPM = Math.Round(cpm, 2),
                        CPV = Math.Round(cpv, 2),
                        CPA = Math.Round(cpa, 2),
                        ROAS = Math.Round(roas, 2)
                    });
                }
            }

        await _db.SaveChangesAsync();
        Console.WriteLine("Data seeded successfully with 3x3x3 structure.");
    }

// (GenerateAndSaveAsync, GenerateGA4DataForClient gibi mevcut diğer metotların
    // aynen burada kalmaya devam edebilir, ben odaklanman için sadece SeedAsync kısmını değiştirdim.)
}