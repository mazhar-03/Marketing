using Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Client> Clients { get; set; }
    public DbSet<PlatformDailyInsight> PlatformDailyInsights { get; set; }
    public DbSet<DailyCampaignKPI> DailyKpis { get; set; }
    public DbSet<GA4DailyInsight> GA4DailyInsights { get; set; }
    public Client Client { get; set; } // bunu ekle
}