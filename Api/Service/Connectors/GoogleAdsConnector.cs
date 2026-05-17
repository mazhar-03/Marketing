using Api.Data.Entities;

namespace Api.Service.Connectors;

public class GoogleAdsConnector : IPlatformConnector
{
    public AdPlatform Platform => AdPlatform.GoogleAds;

    public Task<List<PlatformDailyInsight>> FetchInsightsAsync(Client client, DateTime date)
    {
        // TODO: implement when OAuth credentials are available
        // Will use: Google.Ads.GoogleAds NuGet package
        throw new NotImplementedException("Google Ads credentials not configured yet.");
    }
}