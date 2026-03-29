using Microsoft.EntityFrameworkCore;
using Serilog;
using SubscriptionLeakDetector.Application;
using SubscriptionLeakDetector.Application.Alerts;
using SubscriptionLeakDetector.Application.Subscriptions;
using SubscriptionLeakDetector.Infrastructure;
using SubscriptionLeakDetector.Infrastructure.Persistence;
using SubscriptionLeakDetector.Worker.Jobs;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSerilog((_, lc) =>
    lc.ReadFrom.Configuration(builder.Configuration).Enrich.FromLogContext().WriteTo.Console());

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddHostedService<SubscriptionDetectionJob>();

var host = builder.Build();

using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

host.Run();
