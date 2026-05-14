using Api.Data.Entities;

namespace Api.Service.Connectors;

public interface IPlatformConnector
{
    AdPlatform Platform { get; }
    Task<List<PlatformDailyInsight>> FetchInsightsAsync(Client client, DateTime date);
}