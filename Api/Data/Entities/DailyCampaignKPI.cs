namespace Api.Data.Entities;

public class DailyCampaignKPI
{
    public int Id { get; set; }

    public int ClientId { get; set; }

    public AdPlatform Platform { get; set; } // 2 = Google

    public DateTime Date { get; set; }

    public string CampaignName { get; set; }

    public string AdsetName { get; set; }   // NEW
    public string AdName { get; set; }      // NEW

    public decimal TotalSpend { get; set; }
    public long TotalClicks { get; set; }
    public long TotalImpressions { get; set; }
    public decimal CTR { get; set; }
    public decimal CPC { get; set; }
    public decimal CPM { get; set; }
}