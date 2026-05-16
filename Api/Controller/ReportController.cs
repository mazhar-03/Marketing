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

    // GET /api/clients/1/report/weekly?week=2026-05-12
    // If no week provided, defaults to last Monday
    [HttpGet("weekly")]
    public async Task<IActionResult> GetWeeklyReport(
        int clientId,
        [FromQuery] DateTime? week)
    {
        // Default to last Monday
        var weekStart = week?.Date ?? GetLastMonday();

        var pdfBytes = await _reportService.GenerateWeeklyReportAsync(clientId, weekStart);

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