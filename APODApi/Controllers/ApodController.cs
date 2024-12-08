using APODApi.Core.Interfaces;
using APODApi.Core.Models;
using APODApi.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Cryptography.Xml;

namespace APODApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ApodController : ControllerBase
    {
        private readonly IApodRepository _repository;
        private readonly IMemoryCache _cache;
        private readonly ILogger<ApodController> _logger;
        private readonly IWallpaperService _wallpaperService;
        private readonly INasaApodService _nasaApodService;

        public ApodController(
                IApodRepository repository,
                IMemoryCache cache,
                ILogger<ApodController> logger,
                IWallpaperService wallpaperService,
                INasaApodService nasaApodService)
        {
            _repository = repository;
            _cache = cache;
            _logger = logger;
            _wallpaperService = wallpaperService;
            _nasaApodService = nasaApodService;
        }

        [HttpGet]
        [ResponseCache(CacheProfileName = "Default")]
        [ProducesResponseType(typeof(IEnumerable<ApodEntry>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<ApodEntry>>> GetAll()
        {
            try
            {
                string cacheKey = "apod_all";
                if (_cache.TryGetValue(cacheKey, out IEnumerable<ApodEntry> entries))
                {
                    return Ok(entries);
                }

                entries = await _repository.GetAllAsync();
                var cacheOptions = new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.FromMinutes(5))
                    .SetAbsoluteExpiration(TimeSpan.FromHours(1));

                _cache.Set(cacheKey, entries, cacheOptions);
                return Ok(entries);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching all APOD entries");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    "An error occurred while processing your request");
            }
        }

        [HttpPost("wallpaper/{date}")]
        [ProducesResponseType(typeof(ApodEntry), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApodEntry>> SetWallpaper(string date)
        {
            if (!TryParseAndValidateDate(date, out DateTime parsedDate))
            {
                return BadRequest("Invalid date format. Please use YYYY-MM-DD format.");
            }

            var entry = await GetOrFetchApodEntry(parsedDate);
            if (entry == null)
            {
                return NotFound($"No APOD entry found for date: {date}");
            }

            if (string.IsNullOrEmpty(entry.LocalFilePath))
            {
                return BadRequest("No local file available for this APOD entry");
            }

            await _wallpaperService.SetWallpaperFromLocalPathAsync(entry.LocalFilePath);
            return Ok(entry);
        }

        [HttpGet("{date}")]
        [ResponseCache(CacheProfileName = "Hourly")]
        [ProducesResponseType(typeof(ApodEntry), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApodEntry>> GetByDate(string date)
        {
            if (!TryParseAndValidateDate(date, out DateTime parsedDate))
            {
                return BadRequest("Invalid date format. Please use YYYY-MM-DD format.");
            }

            string cacheKey = $"apod_{date}";
            if (_cache.TryGetValue(cacheKey, out ApodEntry? entry))
            {
                return Ok(entry);
            }

            entry = await GetOrFetchApodEntry(parsedDate);
            if (entry == null)
            {
                return NotFound($"No APOD entry found for date: {date}");
            }

            var cacheOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromHours(1));

            _cache.Set(cacheKey, entry, cacheOptions);
            return Ok(entry);
        }

        [HttpGet("latest")]
        [ResponseCache(CacheProfileName = "Default")]
        [ProducesResponseType(typeof(ApodEntry), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApodEntry>> GetLatest()
        {
            string cacheKey = "apod_latest";
            if (_cache.TryGetValue(cacheKey, out ApodEntry? latest))
            {
                return Ok(latest);
            }

            var entries = await _repository.GetAllAsync();
            latest = entries.MaxBy(e => e.Date);

            if (latest == null)
            {
                return NotFound("No APOD entries found in the database");
            }

            var cacheOptions = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromMinutes(5));

            _cache.Set(cacheKey, latest, cacheOptions);
            return Ok(latest);
        }

        private bool TryParseAndValidateDate(string date, out DateTime parsedDate)
        {
            if (!DateTime.TryParse(date, out parsedDate))
            {
                return false;
            }

            var minDate = new DateTime(1995, 6, 16);
            return parsedDate >= minDate && parsedDate <= DateTime.Today;
        }
        private async Task<ApodEntry?> GetOrFetchApodEntry(DateTime date)
        {
            var entry = await _repository.GetByDateAsync(date);
            if (entry != null)
            {
                return entry;
            }

            var response = await _nasaApodService.GetApodByDateAsync(date.ToString("yyyy-MM-dd"));
            if (response == null)
            {
                return null;
            }

            string? localPath = null;
            if (response.Media_type.Equals("image", StringComparison.OrdinalIgnoreCase))
            {
                localPath = await _wallpaperService.SetWallpaperFromUrlAsync(response.Url, response.Title);
            }

            var newEntry = new ApodEntry
            {
                Title = response.Title,
                Explanation = response.Explanation,
                Url = response.Url,
                MediaType = response.Media_type,
                Date = DateTime.Parse(response.Date).Date,
                CreatedAt = DateTime.UtcNow,
                LocalFilePath = localPath
            };

            await _repository.AddAsync(newEntry);
            return newEntry;
        }

        // Optional: Add a method to manually clear cache if needed
        [HttpPost("clear-cache")]
        [ApiExplorerSettings(IgnoreApi = true)] // Hide from Swagger
        public IActionResult ClearCache()
        {
            if (_cache is MemoryCache memoryCache)
            {
                memoryCache.Compact(1.0);
            }
            return NoContent();
        }
    }
}
