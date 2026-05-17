namespace Api.Data.Entities;

public class GA4DailyInsight
{
    public int Id { get; set; }
    public int ClientId { get; set; }
    public DateTime Date { get; set; }

    // Traffic
    public long Sessions { get; set; }
    public long TotalUsers { get; set; }
    public long NewUsers { get; set; }

    // Engagement
    public decimal BounceRate { get; set; }
    public decimal AvgSessionDuration { get; set; } // seconds
    public long PageViews { get; set; }

    // Source
    public string Source { get; set; } // google, facebook, direct
    public string Medium { get; set; } // cpc, organic, referral
    public string CampaignName { get; set; } // UTM campaign

    // Conversions
    public long Conversions { get; set; }
    public string ConversionEventName { get; set; } // purchase, lead, signup

    // Navigation property
    public Client Client { get; set; }
}