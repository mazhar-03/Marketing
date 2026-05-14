namespace Api.Service.Connectors;

using Data.Entities;

public class MetaConnector : IPlatformConnector
{
    private readonly HttpClient _http;

    public AdPlatform Platform => AdPlatform.Meta;

    public MetaConnector(HttpClient http)
    {
        _http = http;
    }

    public Task<List<PlatformDailyInsight>> FetchInsightsAsync(Client client, DateTime date)
    {
        // TODO: implement when token is available
        // Will call: https://graph.facebook.com/v19.0/{act_id}/insights
        throw new NotImplementedException("Meta token not configured yet.");
    }
}