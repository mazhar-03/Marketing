using Api.Data;
using Api.Middleware;
using Api.Service;
using Api.Service.Connectors;
using Hangfire;
using Hangfire.MemoryStorage;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);
QuestPDF.Settings.License = LicenseType.Community;

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowNextJs", policy =>
    {
        policy.AllowAnyOrigin() // Test ortamı için herkese açık yapıyoruz
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var dataSourceBuilder = new NpgsqlDataSourceBuilder(builder.Configuration.GetConnectionString("DefaultConnection"));
dataSourceBuilder.EnableDynamicJson();
var dataSource = dataSourceBuilder.Build();

// DbContext'i bu ayarlı DataSource ile başlatıyoruz
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(dataSource));

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

builder.Services.AddScoped<FakeDataService>();
builder.Services.AddScoped<KpiService>();
builder.Services.AddHttpClient();
builder.Services.AddScoped<IPlatformConnector, MetaConnector>();
builder.Services.AddScoped<IPlatformConnector, GoogleAdsConnector>();
builder.Services.AddScoped<IPlatformConnector, TikTokConnector>();
builder.Services.AddScoped<IPlatformConnector, LinkedInConnector>();
builder.Services.AddScoped<DataIngestionService>();
builder.Services.AddScoped<GA4IngestionService>();
builder.Services.AddScoped<WeeklyReportService>();

builder.Services.AddHangfire(config => config.UseMemoryStorage());
builder.Services.AddHangfireServer();


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseCors("AllowNextJs"); // Bunu MapControllers'dan önceye koy!

app.UseHttpsRedirection();
app.UseHangfireDashboard("/hangfire");

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var jobManager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();

    jobManager.AddOrUpdate<FakeDataService>(
        "fake-data-job",
        service => service.SeedAsync(),
        "0 0 8 * * *"  // every day at 08:00
    );
    
    jobManager.AddOrUpdate<GA4IngestionService>(
        "ga4-ingestion-job",
        service => service.RunAsync(),
        "0 5 8 * * *"  // 08:05 — fake data 08:00, KPI 08:10
    );

    jobManager.AddOrUpdate<KpiService>(
        "daily-kpi-job",
        service => service.GenerateDailyKpis(DateTime.UtcNow.Date),
        "0 10 8 * * *"  // every day at 08:10
    );
    
    jobManager.AddOrUpdate<DataIngestionService>(
        "data-ingestion-job",
        service => service.RunAsync(),
        "0 0 8 * * *"
    );
}

app.Run();