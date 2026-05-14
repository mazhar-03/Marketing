using Api.Data;
using Api.Data.Entities;
using Api.Data;

namespace Api.Service;

public class MetaFakeDataService
{
    private readonly AppDbContext _db;

    public MetaFakeDataService(AppDbContext db)
    {
        _db = db;
    }

    public async Task GenerateAndSaveAsync()
    {
        var today = DateTime.UtcNow.Date;

        var exists = _db.MetaDailyInsights
            .Any(x => x.Date == today);

        if (exists)
        {
            Console.WriteLine("Already exists for today - skipping");
            return;
        }

        var random = new Random();

        var fake = new MetaDailyInsight
        {
            ClientId = 1,
            Date = today,
            CampaignName = "Campaign " + random.Next(1, 5),
            AdsetName = "Adset " + random.Next(1, 3),
            AdName = "Ad " + random.Next(1, 10),
            Spend = (decimal)(random.NextDouble() * 100),
            Impressions = random.Next(1000, 50000),
            Clicks = random.Next(10, 2000)
        };

        _db.MetaDailyInsights.Add(fake);
        await _db.SaveChangesAsync();

        Console.WriteLine("Inserted clean daily record");
    }}