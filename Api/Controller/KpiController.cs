using Api.Data;
using Api.Data;
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

    [HttpGet]
    public IActionResult Get()
    {
        var data = _db.DailyCampaignKPIs
            .OrderByDescending(x => x.Date)
            .ToList();

        return Ok(data);
    }
}