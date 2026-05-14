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
    public DbSet<DailyCampaignKPI> DailyCampaignKPIs { get; set; }

    public DbSet<MetaDailyInsight> MetaDailyInsights { get; set; }
}