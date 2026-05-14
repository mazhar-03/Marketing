using Api.Data;
using Api.Service;
using Hangfire;
using Hangfire.MemoryStorage;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Controllers
builder.Services.AddControllers();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// DB
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// App Services
builder.Services.AddScoped<MetaFakeDataService>();
builder.Services.AddScoped<KpiService>();

// Hangfire
builder.Services.AddHangfire(config => config.UseMemoryStorage());
builder.Services.AddHangfireServer();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseHangfireDashboard("/hangfire");

app.MapControllers();

// Hangfire Jobs
using (var scope = app.Services.CreateScope())
{
    var jobManager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();

    // Runs every day at 08:00
    jobManager.AddOrUpdate<MetaFakeDataService>(
        "meta-fake-job",
        service => service.GenerateAndSaveAsync(),
        "0 0 8 * * *"
    );

    // Runs every day at 08:10
    jobManager.AddOrUpdate<KpiService>(
        "daily-kpi-job",
        service => service.GenerateDailyKpis(DateTime.UtcNow.Date),
        "0 10 8 * * *"
    );
}
app.Run();