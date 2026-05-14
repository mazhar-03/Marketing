namespace Api.Data.Entities;

public class MetaDailyInsight
{
    public int Id { get; set; }

    public int ClientId { get; set; }

    public DateTime Date { get; set; }

    public string CampaignName { get; set; }

    public string AdsetName { get; set; }

    public string AdName { get; set; }

    public decimal Spend { get; set; }

    public long Impressions { get; set; }

    public long Clicks { get; set; }
}