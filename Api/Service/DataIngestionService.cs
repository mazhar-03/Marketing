using Api.Data;
using Api.Service.Connectors;
using Microsoft.EntityFrameworkCore;

namespace Api.Service;

public class DataIngestionService
{
    private readonly AppDbContext _db;
    private readonly IEnumerable<IPlatformConnector> _connectors;
    private readonly FakeDataService _fakeData;
 
    public DataIngestionService(
        AppDbContext db,
        IEnumerable<IPlatformConnector> connectors,
        FakeDataService fakeData)
    {
        _db = db;
        _connectors = connectors;
        _fakeData = fakeData;
    }
 
    public async Task RunAsync()
    {
        var today = DateTime.UtcNow.Date;
        var clients = await _db.Clients.ToListAsync();
 
        foreach (var client in clients)
        {
            foreach (var connector in _connectors)
            {
                try
                {
                    Console.WriteLine($"[{connector.Platform}] Fetching real data for {client.Name}...");
                    var insights = await connector.FetchInsightsAsync(client, today);
 
                    _db.PlatformDailyInsights.AddRange(insights);
                    await _db.SaveChangesAsync();
 
                    Console.WriteLine($"[{connector.Platform}] Saved {insights.Count} records for {client.Name}");
                }
                catch (NotImplementedException)
                {
                    // Connector not ready yet — fall back to fake data
                    Console.WriteLine($"[{connector.Platform}] Not implemented — using fake data for {client.Name}");
                    await _fakeData.GeneratePlatformDataForClient(client, connector.Platform, today);
                }
                catch (Exception ex)
                {
                    // Real error — log and continue to next platform
                    Console.WriteLine($"[{connector.Platform}] ERROR for {client.Name}: {ex.Message}");
                }
            }
        }
    }
}