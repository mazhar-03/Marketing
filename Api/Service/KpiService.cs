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
        var rawData = await _db.PlatformDailyInsights
            .Where(x => x.Date == date)
            .ToListAsync();

        // Group by client + platform + campaign
        var grouped = rawData.GroupBy(x => new
        {
            x.ClientId,
            x.Platform,
            x.CampaignName
        });

        foreach (var group in grouped)
        {
            // Skip if KPI already exists for this group
            var exists = await _db.DailyKpis.AnyAsync(x =>
                x.ClientId == group.Key.ClientId &&
                x.Platform == group.Key.Platform &&
                x.CampaignName == group.Key.CampaignName &&
                x.Date == date);

            if (exists) continue;

            var spend = group.Sum(x => x.Spend);
            var clicks = group.Sum(x => x.Clicks);
            var impressions = group.Sum(x => x.Impressions);

            var kpi = new DailyCampaignKPI
            {
                ClientId = group.Key.ClientId,
                Platform = group.Key.Platform,
                Date = date,
                CampaignName = group.Key.CampaignName,
                TotalSpend = spend,
                TotalClicks = clicks,
                TotalImpressions = impressions,
                CTR = impressions == 0 ? 0 : Math.Round((decimal)clicks / impressions * 100, 2),
                CPC = clicks == 0 ? 0 : Math.Round(spend / clicks, 2),
                CPM = impressions == 0 ? 0 : Math.Round(spend / impressions * 1000, 2)
            };

            _db.DailyKpis.Add(kpi);
        }

        await _db.SaveChangesAsync();
        Console.WriteLine($"KPIs generated for {date:yyyy-MM-dd}");
    }
}