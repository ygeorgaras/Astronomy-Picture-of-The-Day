using APODApi.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace APODApi.Infrastructure.Services
{
    public class WallpaperService : IWallpaperService
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int SystemParametersInfo(int uAction, int uParam, string lpvParam, int fuWinIni);

        private const int SPI_SETDESKWALLPAPER = 20;
        private const int SPIF_UPDATEINIFILE = 0x01;
        private const int SPIF_SENDCHANGE = 0x02;

        private readonly ILogger<WallpaperService> _logger;
        private readonly HttpClient _httpClient;
        private readonly string _downloadPath;

        public WallpaperService(ILogger<WallpaperService> logger, HttpClient httpClient)
        {
            _logger = logger;
            _httpClient = httpClient;
            _downloadPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
                "APOD"
            );
            Directory.CreateDirectory(_downloadPath);
        }

        public async Task<string> SetWallpaperFromUrlAsync(string imageUrl, string title)
        {
            try
            {
                var imagePath = await DownloadImageAsync(imageUrl, title);
                SetWallpaper(imagePath);
                _logger.LogInformation("Wallpaper set successfully: {Title}", title);
                return imagePath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to set wallpaper from URL: {Url}", imageUrl);
                throw;
            }
        }

        public async Task SetWallpaperFromLocalPathAsync(string localPath)
        {
            if (!File.Exists(localPath))
            {
                throw new FileNotFoundException("Wallpaper image not found", localPath);
            }

            SetWallpaper(localPath);
            await Task.CompletedTask;
        }

        public async Task<string> DownloadImageAsync(string imageUrl, string title)
        {
            var extension = Path.GetExtension(imageUrl);
            if (string.IsNullOrEmpty(extension))
            {
                extension = ".jpg"; // Default to jpg if no extension found
            }

            var safeTitle = string.Join("_", title.Split(Path.GetInvalidFileNameChars()));
            var fileName = $"{DateTime.Now:yyyy-MM-dd}_{safeTitle}{extension}";
            var filePath = Path.Combine(_downloadPath, fileName);

            using var response = await _httpClient.GetAsync(imageUrl);
            response.EnsureSuccessStatusCode();

            using var fileStream = new FileStream(filePath, FileMode.Create);
            await response.Content.CopyToAsync(fileStream);

            return filePath;
        }

        private void SetWallpaper(string imagePath)
        {
            // Set the wallpaper style to stretched in the registry
            using var key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop", true);
            if (key != null)
            {
                key.SetValue("WallpaperStyle", "2");
                key.SetValue("TileWallpaper", "0");
            }

            // Set the wallpaper
            if (SystemParametersInfo(SPI_SETDESKWALLPAPER, 0, imagePath, SPIF_UPDATEINIFILE | SPIF_SENDCHANGE) == 0)
            {
                throw new Exception("Failed to set wallpaper");
            }
        }
    }
}
