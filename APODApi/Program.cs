using APODApi.Core.Interfaces;
using APODApi.Infrastructure.BackgroundServices;
using APODApi.Infrastructure.Data;
using APODApi.Infrastructure.Repository;
using APODApi.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddResponseCaching();
builder.Services.AddMemoryCache();

// Configure controllers to enable response caching
builder.Services.AddControllers(options =>
{
    options.CacheProfiles.Add("Default", new CacheProfile
    {
        Duration = 300 // 5 minutes
    });
    options.CacheProfiles.Add("Hourly", new CacheProfile
    {
        Duration = 3600 // 60 minutes
    });
    options.CacheProfiles.Add("Daily", new CacheProfile
    {
        Duration = 86400 // 24 hours
    });
}); builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure SQLite
builder.Services.AddDbContext<ApodDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHttpClient<INasaApodService, NasaApodService>();
builder.Services.AddScoped<IApodRepository, ApodRepository>();
builder.Services.AddHostedService<ApodBackgroundService>();
builder.Services.AddHttpClient<WallpaperService>();
builder.Services.AddSingleton<IWallpaperService, WallpaperService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseResponseCaching();
app.UseAuthorization();

app.MapControllers();

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApodDbContext>();
    await context.Database.EnsureCreatedAsync();
}

app.Run();
