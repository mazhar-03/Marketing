namespace Api.Data.Entities;

public class Client
{
    public int Id { get; set; }
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
    public string? GA4ServiceAccountJson { get; set; }   // service account key JSON
}