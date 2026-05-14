using Api.Data;
using Api.Data.Entities;
using Api.Data;
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
        var rawData = await _db.MetaDailyInsights
            .Where(x => x.Date == date)
            .ToListAsync();

        var grouped = rawData
            .GroupBy(x => x.CampaignName);

        foreach (var group in grouped)
        {
            var spend = group.Sum(x => x.Spend);
            var clicks = group.Sum(x => x.Clicks);
            var impressions = group.Sum(x => x.Impressions);

            var ctr = impressions == 0 ? 0 : (decimal)clicks / impressions;
            var cpc = clicks == 0 ? 0 : spend / clicks;
            var cpm = clicks == 0 ? 0 : spend / impressions * 1000;

            var kpi = new DailyCampaignKPI
            {
                ClientId = 1,
                Date = date,
                CampaignName = group.Key,
                TotalSpend = spend,
                TotalClicks = clicks,
                TotalImpressions = impressions,
                CTR = ctr,
                CPC = cpc,
                CPM = cpm
            };

            _db.DailyCampaignKPIs.Add(kpi);
        }

        await _db.SaveChangesAsync();

        Console.WriteLine("KPIs generated for " + date.ToShortDateString());
    }
}