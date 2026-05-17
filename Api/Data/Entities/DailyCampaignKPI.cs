using System.ComponentModel.DataAnnotations.Schema;

namespace Api.Data.Entities;

public class DailyCampaignKPI
{
    public int Id { get; set; }
    public int ClientId { get; set; }
    public AdPlatform Platform { get; set; }
    public DateTime Date { get; set; }
    public string CampaignName { get; set; }
    public string AdsetName { get; set; }
    public string AdName { get; set; }

    // Temel Metrikler
    public decimal TotalSpend { get; set; }
    public long TotalClicks { get; set; }
    public long TotalImpressions { get; set; }
    public long TotalViews { get; set; } // YENİ: Video izlenmeleri

    // Dönüşüm Metrikleri
    public decimal TotalConversions { get; set; } // YENİ: Toplam dönüşüm (Ondalıklı olabilir)
    public decimal ConversionValue { get; set; } // YENİ: Dönüşümlerin getirdiği toplam gelir (Ciro)

    // Detaylı Dönüşümler (PostgreSQL JSONB)
    [Column(TypeName = "jsonb")] public Dictionary<string, decimal> ConversionDetails { get; set; } = new();
    // Örnek: { "Purchase": 12.5, "Lead": 4.0, "Add to Cart": 25.0 }

    // Oranlar ve Maliyetler (Hesaplananlar)
    public decimal CTR { get; set; } // Tıklama Oranı (%)
    public decimal CPC { get; set; } // Tıklama Başına Maliyet
    public decimal CPM { get; set; } // Bin Gösterim Başına Maliyet
    public decimal CPV { get; set; } // YENİ: İzlenme Başına Maliyet
    public decimal CPA { get; set; } // YENİ: Dönüşüm Başına Maliyet (Cost Per Action)
    public decimal ROAS { get; set; } // YENİ: Reklam Harcamasının Getirisi (ConversionValue / Spend)
}