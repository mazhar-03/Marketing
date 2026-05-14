using Hangfire;
using Hangfire.MemoryStorage;
using MarketingAnalyticsWorker.Data;
using MarketingAnalyticsWorker.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ---------------- SERVICES ----------------
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// DB
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// App services
builder.Services.AddScoped<MetaFakeDataService>();
builder.Services.AddScoped<KpiService>();

// Hangfire
builder.Services.AddHangfire(config =>
{
    config.UseMemoryStorage();
});
builder.Services.AddHangfireServer();

// ---------------- APP BUILD ----------------
var app = builder.Build();

// Swagger
app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

// ---------------- HANGFIRE JOBS ----------------
using var scope = app.Services.CreateScope();
var jobManager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();

jobManager.AddOrUpdate<MetaFakeDataService>(
    "meta-fake-job",
    service => service.GenerateAndSaveAsync(),
    "*/5 * * * * *"
);

jobManager.AddOrUpdate<KpiService>(
    "daily-kpi-job",
    service => service.GenerateDailyKpis(DateTime.UtcNow.Date),
    "*/10 * * * * *"
);

// ---------------- RUN ----------------
app.Run();