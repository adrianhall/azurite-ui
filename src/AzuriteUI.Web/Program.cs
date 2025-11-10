using AzuriteUI.Web.Controllers.Models;
using AzuriteUI.Web.Extensions;
using AzuriteUI.Web.Filters;
using AzuriteUI.Web.Services.Azurite;
using AzuriteUI.Web.Services.CacheDb;
using AzuriteUI.Web.Services.CacheSync;
using AzuriteUI.Web.Services.Display;
using AzuriteUI.Web.Services.Health;
using AzuriteUI.Web.Services.Repositories;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Get the required connection strings.
var cacheConnectionString = builder.Configuration.GetRequiredConnectionString("CacheDatabase");
var azuriteConnectionString = builder.Configuration.GetRequiredConnectionString("Azurite");

// Cache database context
builder.Services.AddDbContext<CacheDbContext>(options =>
{
    options.UseSqlite(cacheConnectionString, sqliteOptions => sqliteOptions.CommandTimeout(30));
    options.EnableDetailedErrors();
});
builder.Services.AddHostedService<CacheDbInitializer>();

// Azurite service
builder.Services.AddSingleton<IAzuriteService, AzuriteService>();

// Cache synchronization services
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<ICacheSyncService, CacheSyncService>();
builder.Services.AddSingleton<IQueueWorker, QueueWorker>();
builder.Services.AddSingleton<IQueueManager, QueueManager>();
builder.Services.AddHostedService<CacheSyncScheduler>();

// Repositories
builder.Services.AddScoped<IStorageRepository, StorageRepository>();

// OData IEdmModel registration for the OData-based controllers
builder.Services.AddSingleton(ODataModel.BuildEdmModel());

// API Controllers with JSON
builder.Services
    .AddControllers(options =>
    {
        options.Filters.Add<AzuriteExceptionFilter>();
        options.Filters.Add<DtoHeaderFilter>();
    })
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.AllowTrailingCommas = true;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    });

// Razor Pages for UI
builder.Services.AddSingleton<IDisplayHelper, DisplayHelper>();
builder.Services.AddRazorPages();

// Health Checks
builder.Services.AddHealthChecks()
    .AddCheck<AzuriteHealthCheck>("Azurite");

// OpenAPI endpoints
builder.Services
    .AddOutputCache(options => options.AddBasePolicy(policy => policy.Expire(TimeSpan.FromMinutes(15))))
    .AddOpenApi();

// ==================================================================================================

var app = builder.Build();

app.UseStaticFiles();
app.UseRouting();

app.MapRazorPages();
app.MapControllers();
app.MapHealthChecks("/api/health");

// OpenAPI and a nice UI for exploring the API
app.MapOpenApi();
app.MapScalarApiReference();

app.Run();
