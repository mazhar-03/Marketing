using Api.Data.Entities;

namespace Api.Service.Connectors;

public class TikTokConnector : IPlatformConnector
{
    private readonly HttpClient _http;

    public TikTokConnector(HttpClient http)
    {
        _http = http;
    }

    public AdPlatform Platform => AdPlatform.TikTok;

    public Task<List<PlatformDailyInsight>> FetchInsightsAsync(Client client, DateTime date)
    {
        // TODO: implement when token is available
        // Will call: https://business-api.tiktok.com/open_api/v1.3/report/integrated/get/
        throw new NotImplementedException("TikTok token not configured yet.");
    }
}