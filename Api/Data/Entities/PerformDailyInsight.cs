namespace Api.Data.Entities;

public class PlatformDailyInsight
{
    public int Id { get; set; }
    public int ClientId { get; set; }
    public AdPlatform Platform { get; set; }  // hangi platform
    public DateTime Date { get; set; }
    public string CampaignName { get; set; }
    public string AdsetName { get; set; }     // Meta'da adset, Google'da adgroup
    public string AdName { get; set; }
    public decimal Spend { get; set; }
    public long Impressions { get; set; }
    public long Clicks { get; set; }
}