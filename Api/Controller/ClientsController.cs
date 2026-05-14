using Api.Data;
using Api.Data.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api.Controller;

[ApiController]
[Route("api/clients")]
public class ClientsController : ControllerBase
{
    private readonly AppDbContext _db;

    public ClientsController(AppDbContext db)
    {
        _db = db;
    }

    // GET /api/clients
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var clients = await _db.Clients
            .Select(c => new
            {
                c.Id,
                c.Name,
                Platforms = new
                {
                    Meta = c.MetaAdAccountId != null,
                    GoogleAds = c.GoogleAdsCustomerId != null,
                    TikTok = c.TikTokAdvertiserId != null,
                    LinkedIn = c.LinkedInAdAccountId != null,
                    GA4 = c.GA4PropertyId != null
                }
            })
            .ToListAsync();

        return Ok(clients);
    }

    // GET /api/clients/1
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var client = await _db.Clients.FindAsync(id);
        if (client == null) return NotFound();

        // Never return tokens in response
        return Ok(new
        {
            client.Id,
            client.Name,
            client.MetaAdAccountId,
            client.GoogleAdsCustomerId,
            client.TikTokAdvertiserId,
            client.LinkedInAdAccountId,
            client.GA4PropertyId
        });
    }

    // POST /api/clients
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateClientRequest request)
    {
        var client = new Client
        {
            Name = request.Name,
            MetaAdAccountId = request.MetaAdAccountId,
            MetaAccessToken = request.MetaAccessToken,
            GoogleAdsCustomerId = request.GoogleAdsCustomerId,
            GoogleAdsDeveloperToken = request.GoogleAdsDeveloperToken,
            TikTokAdvertiserId = request.TikTokAdvertiserId,
            TikTokAccessToken = request.TikTokAccessToken,
            LinkedInAdAccountId = request.LinkedInAdAccountId,
            LinkedInAccessToken = request.LinkedInAccessToken,
            GA4PropertyId = request.GA4PropertyId,
            GA4ServiceAccountJson = request.GA4ServiceAccountJson
        };

        _db.Clients.Add(client);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = client.Id }, new { client.Id, client.Name });
    }

    // PUT /api/clients/1
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] CreateClientRequest request)
    {
        var client = await _db.Clients.FindAsync(id);
        if (client == null) return NotFound();

        client.Name = request.Name;
        client.MetaAdAccountId = request.MetaAdAccountId;
        client.MetaAccessToken = request.MetaAccessToken;
        client.GoogleAdsCustomerId = request.GoogleAdsCustomerId;
        client.GoogleAdsDeveloperToken = request.GoogleAdsDeveloperToken;
        client.TikTokAdvertiserId = request.TikTokAdvertiserId;
        client.TikTokAccessToken = request.TikTokAccessToken;
        client.LinkedInAdAccountId = request.LinkedInAdAccountId;
        client.LinkedInAccessToken = request.LinkedInAccessToken;
        client.GA4PropertyId = request.GA4PropertyId;
        client.GA4ServiceAccountJson = request.GA4ServiceAccountJson;

        await _db.SaveChangesAsync();

        return NoContent();
    }

    // DELETE /api/clients/1
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var client = await _db.Clients.FindAsync(id);
        if (client == null) return NotFound();

        _db.Clients.Remove(client);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}

public class CreateClientRequest
{
    public string Name { get; set; }

    // Meta
    public string? MetaAdAccountId { get; set; }
    public string? MetaAccessToken { get; set; }

    // Google Ads
    public string? GoogleAdsCustomerId { get; set; }
    public string? GoogleAdsDeveloperToken { get; set; }

    // TikTok
    public string? TikTokAdvertiserId { get; set; }
    public string? TikTokAccessToken { get; set; }

    // LinkedIn
    public string? LinkedInAdAccountId { get; set; }
    public string? LinkedInAccessToken { get; set; }

    // GA4
    public string? GA4PropertyId { get; set; }
    public string? GA4ServiceAccountJson { get; set; }
}