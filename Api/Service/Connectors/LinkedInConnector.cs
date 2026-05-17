using Api.Data.Entities;

namespace Api.Service.Connectors;

public class LinkedInConnector : IPlatformConnector
{
    private readonly HttpClient _http;

    public LinkedInConnector(HttpClient http)
    {
        _http = http;
    }

    public AdPlatform Platform => AdPlatform.LinkedIn;

    public Task<List<PlatformDailyInsight>> FetchInsightsAsync(Client client, DateTime date)
    {
        // TODO: implement when OAuth token is available
        // Will call: https://api.linkedin.com/v2/adAnalytics
        throw new NotImplementedException("LinkedIn token not configured yet.");
    }
}