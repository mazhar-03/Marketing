using Api.Data;
using Api.Data.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api.Controller;

[ApiController]
[Route("api/kpi")]
public class KpiController : ControllerBase
{
    private readonly AppDbContext _db;

    public KpiController(AppDbContext db)
    {
        _db = db;
    }

    // GET /api/kpi
    // GET /api/kpi?clientId=1
    // GET /api/kpi?clientId=1&platform=Meta
    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] int? clientId,
        [FromQuery] AdPlatform? platform)
    {
        var query = _db.DailyKpis.AsQueryable();

        if (clientId.HasValue)
            query = query.Where(x => x.ClientId == clientId);

        if (platform.HasValue)
            query = query.Where(x => x.Platform == platform);

        var data = await query
            .OrderByDescending(x => x.Date)
            .ToListAsync();

        return Ok(data);
    }
}