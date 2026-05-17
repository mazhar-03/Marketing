using Api.Data;
using Api.Data.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api.Controller;

[ApiController]
[Route("api/clients/{clientId}")]
public class InsightsController : ControllerBase
{
    private readonly AppDbContext _db;

    public InsightsController(AppDbContext db)
    {
        _db = db;
    }

    // GET /api/clients/1/insights?from=2026-05-01&to=2026-05-15&platform=Meta
    [HttpGet("insights")]
    public async Task<IActionResult> GetInsights(
        int clientId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] AdPlatform? platform)
    {
        var client = await _db.Clients.FindAsync(clientId);
        if (client == null) return NotFound($"Client {clientId} not found.");

        var fromDate = from ?? DateTime.UtcNow.Date.AddDays(-30);
        var toDate = to ?? DateTime.UtcNow.Date;

        var query = _db.PlatformDailyInsights
            .Where(x => x.ClientId == clientId
                        && x.Date >= fromDate
                        && x.Date <= toDate);

        if (platform.HasValue)
            query = query.Where(x => x.Platform == platform);

        var insights = await query
            .OrderByDescending(x => x.Date)
            .ToListAsync();

        return Ok(insights);
    }

    // GET /api/clients/1/ga4?from=2026-05-01&to=2026-05-15&source=google
    [HttpGet("ga4")]
    public async Task<IActionResult> GetGA4(
        int clientId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string? source,
        [FromQuery] string? medium)
    {
        var client = await _db.Clients.FindAsync(clientId);
        if (client == null) return NotFound($"Client {clientId} not found.");

        var fromDate = from ?? DateTime.UtcNow.Date.AddDays(-30);
        var toDate = to ?? DateTime.UtcNow.Date;

        var query = _db.GA4DailyInsights
            .Where(x => x.ClientId == clientId
                        && x.Date >= fromDate
                        && x.Date <= toDate);

        if (!string.IsNullOrEmpty(source))
            query = query.Where(x => x.Source == source);

        if (!string.IsNullOrEmpty(medium))
            query = query.Where(x => x.Medium == medium);

        var data = await query
            .OrderByDescending(x => x.Date)
            .ToListAsync();

        return Ok(data);
    }

    // GET /api/clients/1/summary?from=2026-05-01&to=2026-05-15
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(
        int clientId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
    {
        var client = await _db.Clients.FindAsync(clientId);
        if (client == null) return NotFound($"Client {clientId} not found.");

        var fromDate = from ?? DateTime.UtcNow.Date.AddDays(-30);
        var toDate = to ?? DateTime.UtcNow.Date;

        // Paid media summary per platform
        var kpis = await _db.DailyKpis
            .Where(x => x.ClientId == clientId
                        && x.Date >= fromDate
                        && x.Date <= toDate)
            .ToListAsync();

        var platformSummaries = kpis
            .GroupBy(x => x.Platform)
            .Select(g => new
            {
                Platform = g.Key.ToString(),
                TotalSpend = Math.Round(g.Sum(x => x.TotalSpend), 2),
                TotalClicks = g.Sum(x => x.TotalClicks),
                TotalImpressions = g.Sum(x => x.TotalImpressions),
                AvgCTR = Math.Round(g.Average(x => x.CTR), 2),
                AvgCPC = Math.Round(g.Average(x => x.CPC), 2),
                AvgCPM = Math.Round(g.Average(x => x.CPM), 2),
                TopCampaign = g.GroupBy(x => x.CampaignName)
                    .OrderByDescending(c => c.Sum(x => x.TotalSpend))
                    .Select(c => c.Key)
                    .FirstOrDefault()
            })
            .ToList();

        // GA4 summary
        var ga4 = await _db.GA4DailyInsights
            .Where(x => x.ClientId == clientId
                        && x.Date >= fromDate
                        && x.Date <= toDate)
            .ToListAsync();

        var ga4Summary = ga4.Any()
            ? new
            {
                TotalSessions = ga4.Sum(x => x.Sessions),
                TotalUsers = ga4.Sum(x => x.TotalUsers),
                TotalNewUsers = ga4.Sum(x => x.NewUsers),
                TotalPageViews = ga4.Sum(x => x.PageViews),
                TotalConversions = ga4.Sum(x => x.Conversions),
                AvgBounceRate = Math.Round(ga4.Average(x => x.BounceRate) * 100, 2),
                AvgSessionDuration = Math.Round(ga4.Average(x => x.AvgSessionDuration), 0),
                TopSource = ga4.GroupBy(x => x.Source)
                    .OrderByDescending(g => g.Sum(x => x.Sessions))
                    .Select(g => g.Key)
                    .FirstOrDefault()
            }
            : null;

        return Ok(new
        {
            ClientId = clientId,
            ClientName = client.Name,
            From = fromDate,
            To = toDate,
            PaidMedia = platformSummaries,
            GA4 = ga4Summary
        });
    }

    // GET /api/clients/1/kpi?from=2026-05-01&to=2026-05-15&platform=Meta
    [HttpGet("kpi")]
    public async Task<IActionResult> GetKpi(
        int clientId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] AdPlatform? platform)
    {
        var client = await _db.Clients.FindAsync(clientId);
        if (client == null) return NotFound($"Client {clientId} not found.");

        var fromDate = from ?? DateTime.UtcNow.Date.AddDays(-30);
        var toDate = to ?? DateTime.UtcNow.Date;

        var query = _db.DailyKpis
            .Where(x => x.ClientId == clientId
                        && x.Date >= fromDate
                        && x.Date <= toDate);

        if (platform.HasValue)
            query = query.Where(x => x.Platform == platform);

        var data = await query
            .OrderByDescending(x => x.Date)
            .ToListAsync();

        return Ok(data);
    }
}