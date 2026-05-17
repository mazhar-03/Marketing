using Api.Service;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controller;

[ApiController]
[Route("api/clients/{clientId}/report")]
public class ReportController : ControllerBase
{
    private readonly WeeklyReportService _reportService;

    public ReportController(WeeklyReportService reportService)
    {
        _reportService = reportService;
    }

    // GET /api/clients/1/report/weekly?week=2026-05-10&markup=1.5&metrics=totalSpend,totalClicks,ctr,roas
    [HttpGet("weekly")]
    public async Task<IActionResult> GetWeeklyReport(
        int clientId,
        [FromQuery] DateTime? week,
        [FromQuery] decimal markup = 1,
        [FromQuery] string? metrics = null)
    {
        var weekStart = week?.Date ?? GetLastMonday();
        weekStart = DateTime.SpecifyKind(weekStart, DateTimeKind.Utc);

        // Parse selected metrics — fallback to defaults if not provided
        var selectedMetrics = string.IsNullOrEmpty(metrics)
            ? new List<string> { "totalSpend", "totalClicks", "totalImpressions", "totalConversions" }
            : metrics.Split(",").Select(m => m.Trim()).ToList();

        var pdfBytes = await _reportService.GenerateWeeklyReportAsync(clientId, weekStart, markup, selectedMetrics);

        var fileName = $"google-ads-report_{weekStart:yyyy-MM-dd}.pdf";
        return File(pdfBytes, "application/pdf", fileName);
    }

    private static DateTime GetLastMonday()
    {
        var today = DateTime.UtcNow.Date;
        var daysAgo = ((int)today.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        return daysAgo == 0 ? today.AddDays(-7) : today.AddDays(-daysAgo);
    }
}