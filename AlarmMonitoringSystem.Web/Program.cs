// AlarmMonitoringSystem.Web/Program.cs
using AlarmMonitoringSystem.Infrastructure.Data.Extensions;
using AlarmMonitoringSystem.Web.Extensions;
using AlarmMonitoringSystem.Web.Hubs; // ✅ ADD: Hub namespace
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .WriteTo.Console()
    .WriteTo.File("logs/alarmmonitoring-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllersWithViews();

// ✅ SignalR is now added via AddAlarmMonitoringServices()

// Add our custom services
builder.Services.AddAlarmMonitoringServices(builder.Configuration);

// Add Swagger for API testing
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Migrate database on startup
await app.MigrateDbAsync();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

// Configure routes
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// ✅ ADD: Map SignalR hub
app.MapHub<AlarmMonitoringHub>("/alarmHub");

// Log startup
Log.Information("Alarm Monitoring System starting up...");

app.Run();

// Ensure to flush and stop internal timers/threads before application-exit
Log.CloseAndFlush();