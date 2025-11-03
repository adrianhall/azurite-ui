using AzuriteUI.Web.Extensions;
using AzuriteUI.Web.Services.Azurite;
using AzuriteUI.Web.Services.CacheDb;
using AzuriteUI.Web.Services.CacheSync;
using AzuriteUI.Web.Services.Health;
using AzuriteUI.Web.Services.Repositories;
using AzuriteUI.Web.Services.Repositories.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.OData.ModelBuilder;
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
ODataConventionModelBuilder odataBuilder = new();
odataBuilder.EnableLowerCamelCase();

// Configuration of the ContainerDTO entity
var containerEntity = odataBuilder.EntitySet<ContainerDTO>("Containers").EntityType;
containerEntity.HasKey(c => c.Name);
containerEntity.Ignore(c => c.Metadata); // Ignore Metadata dictionary for OData $select

// Configuration of the BlobDTO entity
var blobEntity = odataBuilder.EntitySet<BlobDTO>("Blobs").EntityType;
blobEntity.HasKey(b => new { b.ContainerName, b.Name });
blobEntity.Ignore(b => b.Metadata); // Ignore Metadata dictionary for OData $select
blobEntity.Ignore(b => b.Tags); // Ignore Tags dictionary for OData $select

// Configuration of the UploadDTO entity
var uploadEntity = odataBuilder.EntitySet<UploadDTO>("Uploads").EntityType;
uploadEntity.HasKey(u => u.Id);

// Add the IEdmModel to services
builder.Services.AddSingleton(odataBuilder.GetEdmModel());

// API Controllers with JSON
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.AllowTrailingCommas = true;
    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    options.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
    options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});

// Health Checks
builder.Services.AddHealthChecks()
    .AddCheck<AzuriteHealthCheck>("Azurite");

// OpenAPI endpoints
builder.Services
    .AddOutputCache(options => options.AddBasePolicy(policy => policy.Expire(TimeSpan.FromMinutes(15))))
    .AddOpenApi();

// ==================================================================================================

var app = builder.Build();

app.UseRouting();
app.MapControllers();
app.MapHealthChecks("/api/health");

// OpenAPI and a nice UI for exploring the API
app.MapOpenApi();
app.MapScalarApiReference();

app.Run();
