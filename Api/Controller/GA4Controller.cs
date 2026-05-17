using Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api.Controller;

[ApiController]
[Route("api/ga4")]
public class GA4Controller : ControllerBase
{
    private readonly AppDbContext _db;

    public GA4Controller(AppDbContext db)
    {
        _db = db;
    }

    // GET /api/ga4
    // GET /api/ga4?clientId=1
    // GET /api/ga4?clientId=1&source=google
    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] int? clientId,
        [FromQuery] string? source,
        [FromQuery] string? medium)
    {
        var query = _db.GA4DailyInsights.AsQueryable();

        if (clientId.HasValue)
            query = query.Where(x => x.ClientId == clientId);

        if (!string.IsNullOrEmpty(source))
            query = query.Where(x => x.Source == source);

        if (!string.IsNullOrEmpty(medium))
            query = query.Where(x => x.Medium == medium);

        var data = await query
            .OrderByDescending(x => x.Date)
            .ToListAsync();

        return Ok(data);
    }
}