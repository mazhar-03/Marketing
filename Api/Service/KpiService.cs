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
                CTR = impressions == 0 ? 0 : (decimal)clicks / impressions * 100,
                CPC = clicks == 0 ? 0 : spend / clicks,
                CPM = impressions == 0 ? 0 : spend / impressions * 1000
            });
        }

        await _db.SaveChangesAsync();
    }}