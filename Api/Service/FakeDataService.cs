using Api.Data;
using Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Api.Service;

public class FakeDataService
{
    private readonly AppDbContext _db;
    private readonly Random _random = new();

    private readonly string[] _sources = { "google", "facebook", "tiktok", "direct", "linkedin" };
    private readonly string[] _mediums = { "cpc", "organic", "referral", "email" };
    private readonly string[] _campaigns = { "brand_awareness", "retargeting", "summer_sale", "lead_gen" };
    private readonly string[] _conversionEvents = { "purchase", "lead", "signup", "add_to_cart" };

    public FakeDataService(AppDbContext db)
    {
        _db = db;
    }

    public async Task GenerateAndSaveAsync()
    {
        var today = DateTime.UtcNow.Date;
        var clients = await _db.Clients.ToListAsync();

        foreach (var client in clients)
        {
            await GenerateAdPlatformData(client, today);
            await GenerateGA4Data(client, today);
        }

        await _db.SaveChangesAsync();
    }

    private async Task GenerateAdPlatformData(Client client, DateTime date)
    {
        foreach (var platform in Enum.GetValues<AdPlatform>())
        {
            var exists = await _db.PlatformDailyInsights
                .AnyAsync(x => x.ClientId == client.Id
                            && x.Platform == platform
                            && x.Date == date);

            if (exists)
            {
                Console.WriteLine($"[{platform}] {client.Name} — already exists, skipping");
                continue;
            }

            var campaignCount = _random.Next(2, 5);
            for (int i = 0; i < campaignCount; i++)
            {
                _db.PlatformDailyInsights.Add(new PlatformDailyInsight
                {
                    ClientId = client.Id,
                    Platform = platform,
                    Date = date,
                    CampaignName = $"{platform} Campaign {_random.Next(1, 6)}",
                    AdsetName = $"Adset {_random.Next(1, 4)}",
                    AdName = $"Ad {_random.Next(1, 10)}",
                    Spend = Math.Round((decimal)(_random.NextDouble() * 500), 2),
                    Impressions = _random.Next(1000, 100000),
                    Clicks = _random.Next(10, 5000)
                });
            }

            Console.WriteLine($"[{platform}] Generated fake data for {client.Name}");
        }
    }

    private async Task GenerateGA4Data(Client client, DateTime date)
    {
        var exists = await _db.GA4DailyInsights
            .AnyAsync(x => x.ClientId == client.Id && x.Date == date);

        if (exists)
        {
            Console.WriteLine($"[GA4] {client.Name} — already exists, skipping");
            return;
        }

        // Multiple rows per day — one per source/medium combo
        var rows = _random.Next(3, 7);
        for (int i = 0; i < rows; i++)
        {
            var sessions = _random.Next(100, 5000);
            var totalUsers = (long)(sessions * (_random.NextDouble() * 0.9 + 0.1));
            var newUsers = (long)(totalUsers * (_random.NextDouble() * 0.6 + 0.2));

            _db.GA4DailyInsights.Add(new GA4DailyInsight
            {
                ClientId = client.Id,
                Date = date,
                Sessions = sessions,
                TotalUsers = totalUsers,
                NewUsers = newUsers,
                BounceRate = Math.Round((decimal)(_random.NextDouble() * 0.6 + 0.2), 4),
                AvgSessionDuration = Math.Round((decimal)(_random.NextDouble() * 180 + 30), 2),
                PageViews = _random.Next(sessions, sessions * 5),
                Source = _sources[_random.Next(_sources.Length)],
                Medium = _mediums[_random.Next(_mediums.Length)],
                CampaignName = _campaigns[_random.Next(_campaigns.Length)],
                Conversions = _random.Next(0, 200),
                ConversionEventName = _conversionEvents[_random.Next(_conversionEvents.Length)]
            });
        }

        Console.WriteLine($"[GA4] Generated fake data for {client.Name}");
    }
    
    public async Task GeneratePlatformDataForClient(Client client, AdPlatform platform, DateTime date)
    {
        // mevcut GenerateAdPlatformData metodunun içeriği buraya
        // sadece tek bir platform için çalışacak şekilde
    }
}