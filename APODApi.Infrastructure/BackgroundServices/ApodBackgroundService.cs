using APODApi.Core.Interfaces;
using APODApi.Core.Models;
using APODApi.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APODApi.Infrastructure.BackgroundServices
{
    public class ApodBackgroundService : BackgroundService
    {
        private readonly ILogger<ApodBackgroundService> _logger;
        private readonly IServiceProvider _serviceProvider;

        public ApodBackgroundService(
            ILogger<ApodBackgroundService> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var nasaService = scope.ServiceProvider.GetRequiredService<INasaApodService>();
                    var repository = scope.ServiceProvider.GetRequiredService<IApodRepository>();
                    var wallpaperService = scope.ServiceProvider.GetRequiredService<IWallpaperService>();

                    var today = DateTime.UtcNow.Date;
                    if (!await repository.ExistsAsync(today))
                    {
                        var apodResponse = await nasaService.GetLatestApodAsync();
                        string? localPath = null;

                        // Only set wallpaper for images, not videos
                        if (apodResponse.Media_type.Equals("image", StringComparison.OrdinalIgnoreCase))
                        {
                            localPath = await wallpaperService.SetWallpaperFromUrlAsync(apodResponse.Url, apodResponse.Title);
                        }

                        var entry = new ApodEntry
                        {
                            Title = apodResponse.Title,
                            Explanation = apodResponse.Explanation,
                            Url = apodResponse.Url,
                            MediaType = apodResponse.Media_type,
                            Date = DateTime.Parse(apodResponse.Date).Date,
                            CreatedAt = DateTime.UtcNow,
                            LocalFilePath = localPath
                        };

                        await repository.AddAsync(entry);
                        _logger.LogInformation("Added new APOD entry for {Date}", today);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while fetching APOD");
                }

                // Wait for 24 hours before checking again
                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }
        }
    }
}
