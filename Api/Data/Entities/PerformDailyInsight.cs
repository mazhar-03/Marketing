namespace Api.Data.Entities;

public class PlatformDailyInsight
{
    public int Id { get; set; }
    public int ClientId { get; set; }
    public AdPlatform Platform { get; set; }
    public DateTime Date { get; set; }
    public string CampaignName { get; set; }
    public string AdsetName { get; set; }
    public string AdName { get; set; }
    public decimal Spend { get; set; }
    public long Impressions { get; set; }
    public long Clicks { get; set; }
    public long Views { get; set; } // Ham izlenme sayısı
    public decimal Conversions { get; set; } // Ham dönüşüm sayısı
    public decimal ConversionValue { get; set; }
} // YENİ: Reklam Harcamasının Getirisi (ConversionValue / Spend)