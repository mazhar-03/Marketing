using Api.Data;
using Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Api.Service;

public class GA4IngestionService
{
    private readonly AppDbContext _db;
    private readonly FakeDataService _fakeData;

    public GA4IngestionService(AppDbContext db, FakeDataService fakeData)
    {
        _db = db;
        _fakeData = fakeData;
    }

    public async Task RunAsync()
    {
        var today = DateTime.UtcNow.Date;
        var clients = await _db.Clients.ToListAsync();

        foreach (var client in clients)
        {
            try
            {
                if (string.IsNullOrEmpty(client.GA4PropertyId) ||
                    string.IsNullOrEmpty(client.GA4ServiceAccountJson))
                {
                    Console.WriteLine($"[GA4] {client.Name} — no credentials, using fake data");
                    await _fakeData.GenerateGA4DataForClient(client, today);
                    continue;
                }

                // TODO: implement real GA4 fetch when credentials are available
                // Will use: Google.Analytics.Data.V1Beta NuGet package
                // var analyticsData = new BetaAnalyticsDataClient(...);
                // var response = await analyticsData.RunReportAsync(...);
                throw new NotImplementedException();
            }
            catch (NotImplementedException)
            {
                Console.WriteLine($"[GA4] {client.Name} — not implemented yet, using fake data");
                await _fakeData.GenerateGA4DataForClient(client, today);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GA4] ERROR for {client.Name}: {ex.Message}");
            }
        }

        await _db.SaveChangesAsync();
    }
}