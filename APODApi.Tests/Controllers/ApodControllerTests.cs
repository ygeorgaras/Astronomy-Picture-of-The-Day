using APODApi.Controllers;
using APODApi.Core.Interfaces;
using APODApi.Core.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;

namespace APODApi.Tests.Controllers
{
    public class ApodControllerTests
    {
        private readonly Mock<IApodRepository> _mockRepository;
        private readonly Mock<ILogger<ApodController>> _mockLogger;
        private readonly Mock<IWallpaperService> _mockWallpaperService;
        private readonly Mock<INasaApodService> _mockNasaService;
        private readonly IMemoryCache _cache;
        private readonly ApodController _controller;

        public ApodControllerTests()
        {
            _mockRepository = new Mock<IApodRepository>();
            _mockLogger = new Mock<ILogger<ApodController>>();
            _mockWallpaperService = new Mock<IWallpaperService>();
            _mockNasaService = new Mock<INasaApodService>();
            _cache = new MemoryCache(new MemoryCacheOptions());

            _controller = new ApodController(
                _mockRepository.Object,
                _cache,
                _mockLogger.Object,
                _mockWallpaperService.Object,
                _mockNasaService.Object
            );
        }

        [Fact]
        public async Task GetAll_ReturnsOkResult_WithEntries()
        {
            // Arrange
            var expectedEntries = new List<ApodEntry>
        {
            new() { Id = 1, Title = "Test 1", Date = DateTime.Today },
            new() { Id = 2, Title = "Test 2", Date = DateTime.Today.AddDays(-1) }
        };

            _mockRepository.Setup(repo => repo.GetAllAsync())
                .ReturnsAsync(expectedEntries);

            // Act
            var result = await _controller.GetAll();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedEntries = Assert.IsAssignableFrom<IEnumerable<ApodEntry>>(okResult.Value);
            returnedEntries.Should().BeEquivalentTo(expectedEntries);
        }

        [Fact]
        public async Task SetWallpaper_WithValidDate_ReturnsOkResult()
        {
            // Arrange
            var date = "2024-01-01";
            var expectedEntry = new ApodEntry
            {
                Id = 1,
                Title = "Test",
                Date = DateTime.Parse(date),
                LocalFilePath = "C:/test/image.jpg"
            };

            _mockRepository.Setup(repo => repo.GetByDateAsync(It.IsAny<DateTime>()))
                .ReturnsAsync(expectedEntry);
            _mockWallpaperService.Setup(w => w.SetWallpaperFromLocalPathAsync(It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.SetWallpaper(date);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedEntry = Assert.IsType<ApodEntry>(okResult.Value);
            returnedEntry.Should().BeEquivalentTo(expectedEntry);
        }

        [Fact]
        public async Task SetWallpaper_WithInvalidDate_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.SetWallpaper("invalid-date");

            // Assert
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task GetAll_WhenRepositoryThrows_Returns500()
        {
            // Arrange
            _mockRepository.Setup(repo => repo.GetAllAsync())
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.GetAll();

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status500InternalServerError, objectResult.StatusCode);
            Assert.Equal("An error occurred while processing your request", objectResult.Value);

            // Verify that the error was logged
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => true),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
                Times.Once);
        }

        [Fact]
        public void ClearCache_ClearsMemoryCache()
        {
            // Arrange
            string cacheKey = "test_key";
            _cache.Set(cacheKey, "test_value");

            // Act
            var result = _controller.ClearCache();

            // Assert
            Assert.IsType<NoContentResult>(result);
            Assert.Null(_cache.Get(cacheKey));
        }
        [Fact]
        public async Task GetByDate_WithValidDate_ReturnsOkResult()
        {
            // Arrange
            var date = "2024-01-01";
            var expectedEntry = new ApodEntry
            {
                Id = 1,
                Title = "Test",
                Date = DateTime.Parse(date)
            };

            _mockRepository.Setup(repo => repo.GetByDateAsync(It.IsAny<DateTime>()))
                .ReturnsAsync(expectedEntry);

            // Act
            var result = await _controller.GetByDate(date);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedEntry = Assert.IsType<ApodEntry>(okResult.Value);
            returnedEntry.Should().BeEquivalentTo(expectedEntry);
        }

        [Fact]
        public async Task GetByDate_WithInvalidDate_ReturnsBadRequest()
        {
            // Arrange
            var invalidDate = "invalid-date";

            // Act
            var result = await _controller.GetByDate(invalidDate);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task GetByDate_WithNonExistentDate_ReturnsNotFound()
        {
            // Arrange
            var date = "2024-01-01";
            _mockRepository.Setup(repo => repo.GetByDateAsync(It.IsAny<DateTime>()))
                .ReturnsAsync((ApodEntry?)null);

            // Act
            var result = await _controller.GetByDate(date);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result.Result);
        }

        [Fact]
        public async Task GetLatest_WithEntries_ReturnsLatestEntry()
        {
            // Arrange
            var entries = new List<ApodEntry>
        {
            new() { Id = 1, Title = "Old", Date = DateTime.Today.AddDays(-1) },
            new() { Id = 2, Title = "Latest", Date = DateTime.Today }
        };

            _mockRepository.Setup(repo => repo.GetAllAsync())
                .ReturnsAsync(entries);

            // Act
            var result = await _controller.GetLatest();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedEntry = Assert.IsType<ApodEntry>(okResult.Value);
            returnedEntry.Date.Should().Be(DateTime.Today);
            returnedEntry.Title.Should().Be("Latest");
        }
    }
}
