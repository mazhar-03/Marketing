using Api.Service;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controller;

[ApiController]
[Route("api/seed")]
public class SeedController : ControllerBase
{
    private readonly FakeDataService _fakeData;
    private readonly KpiService _kpi;

    public SeedController(FakeDataService fakeData, KpiService kpi)
    {
        _fakeData = fakeData;
        _kpi = kpi;
    }

    // POST /api/seed
    [HttpPost]
    public async Task<IActionResult> Seed()
    {
        if (!HttpContext.RequestServices
                .GetRequiredService<IWebHostEnvironment>()
                .IsDevelopment())
            return Forbid();
        
        await _fakeData.SeedAsync();
        await _kpi.GenerateDailyKpis(DateTime.UtcNow.Date);
        return Ok("Seeded successfully.");
    }
    
    [HttpPost("seed-kpis")]
    public async Task<IActionResult> SeedGoogle()
    {
        await _fakeData.SeedAsync();
        return Ok("Seed completed");
    }
}