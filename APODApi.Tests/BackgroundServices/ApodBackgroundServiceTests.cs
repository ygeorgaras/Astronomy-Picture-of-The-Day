using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using APODApi.Core.Interfaces;
using APODApi.Core.Models;
using APODApi.Infrastructure.BackgroundServices;

namespace APODApi.Tests.BackgroundServices;

public class ApodBackgroundServiceTests
{
    private readonly Mock<ILogger<ApodBackgroundService>> _mockLogger;
    private readonly Mock<IServiceProvider> _mockServiceProvider;
    private readonly Mock<INasaApodService> _mockNasaService;
    private readonly Mock<IApodRepository> _mockRepository;
    private readonly Mock<IWallpaperService> _mockWallpaperService;

    public ApodBackgroundServiceTests()
    {
        _mockLogger = new Mock<ILogger<ApodBackgroundService>>();
        _mockServiceProvider = new Mock<IServiceProvider>();
        _mockNasaService = new Mock<INasaApodService>();
        _mockRepository = new Mock<IApodRepository>();
        _mockWallpaperService = new Mock<IWallpaperService>();

        SetupServiceProvider();
    }

    [Fact]
    public async Task ExecuteAsync_WhenNewImageAvailable_SavesToRepositoryAndSetsWallpaper()
    {
        // Arrange
        var testDate = DateTime.UtcNow.Date;
        var testResponse = new NasaApodResponse
        {
            Title = "Test Image",
            Explanation = "Test Description",
            Url = "http://example.com/image.jpg",
            Media_type = "image",
            Date = testDate.ToString("yyyy-MM-dd")
        };

        _mockRepository.Setup(r => r.ExistsAsync(testDate))
            .ReturnsAsync(false);

        _mockNasaService.Setup(s => s.GetLatestApodAsync())
            .ReturnsAsync(testResponse);

        _mockWallpaperService.Setup(w => w.SetWallpaperFromUrlAsync(testResponse.Url, testResponse.Title))
            .ReturnsAsync("C:/test/image.jpg");

        var service = new ApodBackgroundService(_mockLogger.Object, _mockServiceProvider.Object);
        using var cts = new CancellationTokenSource();

        // Act
        var task = service.StartAsync(cts.Token);
        await Task.Delay(100); // Give the service time to execute
        cts.Cancel();
        await task;

        // Assert
        _mockRepository.Verify(r => r.AddAsync(It.Is<ApodEntry>(e =>
            e.Title == testResponse.Title &&
            e.Url == testResponse.Url &&
            e.Date == testDate &&
            e.MediaType == testResponse.Media_type)), Times.Once);

        _mockWallpaperService.Verify(w =>
            w.SetWallpaperFromUrlAsync(testResponse.Url, testResponse.Title), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WhenEntryExists_DoesNotAddNewEntry()
    {
        // Arrange
        var testDate = DateTime.UtcNow.Date;

        _mockRepository.Setup(r => r.ExistsAsync(testDate))
            .ReturnsAsync(true);

        var service = new ApodBackgroundService(_mockLogger.Object, _mockServiceProvider.Object);
        using var cts = new CancellationTokenSource();

        // Act
        var task = service.StartAsync(cts.Token);
        await Task.Delay(100);
        cts.Cancel();
        await task;

        // Assert
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<ApodEntry>()), Times.Never);
        _mockNasaService.Verify(s => s.GetLatestApodAsync(), Times.Never);
    }

    private void SetupServiceProvider()
    {
        var mockScope = new Mock<IServiceScope>();
        var mockScopeFactory = new Mock<IServiceScopeFactory>();
        var scopedProvider = new Mock<IServiceProvider>();

        // Setup scoped services
        scopedProvider.Setup(s => s.GetService(typeof(INasaApodService)))
            .Returns(_mockNasaService.Object);
        scopedProvider.Setup(s => s.GetService(typeof(IApodRepository)))
            .Returns(_mockRepository.Object);
        scopedProvider.Setup(s => s.GetService(typeof(IWallpaperService)))
            .Returns(_mockWallpaperService.Object);

        // Setup scope
        mockScope.Setup(s => s.ServiceProvider).Returns(scopedProvider.Object);
        mockScopeFactory.Setup(s => s.CreateScope()).Returns(mockScope.Object);

        // Setup root provider
        _mockServiceProvider.Setup(s => s.GetService(typeof(IServiceScopeFactory)))
            .Returns(mockScopeFactory.Object);
    }
}