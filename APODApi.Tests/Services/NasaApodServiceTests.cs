using APODApi.Core.Models;
using APODApi.Infrastructure.Services;
using Moq.Protected;
using Moq;
using System.Net;
using Microsoft.Extensions.Configuration;
using FluentAssertions;
using System.Text.Json;

namespace APODApi.Tests.Services
{
    public class NasaApodServiceTests
    {
        private readonly Mock<IConfiguration> _mockConfig;
        private readonly Mock<HttpMessageHandler> _mockHttpHandler;
        private readonly string _apiKey = "test-api-key";

        public NasaApodServiceTests()
        {
            _mockConfig = new Mock<IConfiguration>();
            _mockConfig.Setup(c => c["NASA:ApiKey"]).Returns(_apiKey);
            _mockHttpHandler = new Mock<HttpMessageHandler>();
        }

        [Fact]
        public async Task GetApodAsync_SuccessfulResponse_ReturnsApodData()
        {
            // Arrange
            var date = "2024-01-01";
            var response = new NasaApodResponse
            {
                Title = "Test Image",
                Date = DateTime.Today.ToString("yyyy-MM-dd"),
                Url = "https://example.com/image.jpg"
            };

            var httpClient = SetupMockHttpClient(response, HttpStatusCode.OK);
            var service = new NasaApodService(httpClient, _mockConfig.Object);

            // Act
            var result = await service.GetApodByDateAsync(date);

            // Assert
            result.Should().NotBeNull();
            result.Title.Should().Be(response.Title);
        }

        [Fact]
        public async Task GetApodByDateAsync_ApiError_ThrowsException()
        {
            // Arrange
            var httpClient = SetupMockHttpClient(null, HttpStatusCode.InternalServerError);
            var service = new NasaApodService(httpClient, _mockConfig.Object);

            // Act & Assert
            await Assert.ThrowsAsync<HttpRequestException>(
                () => service.GetApodByDateAsync("2024-01-01"));
        }

        [Fact]
        public void Constructor_MissingApiKey_ThrowsException()
        {
            // Arrange
            _mockConfig.Setup(c => c["NASA:ApiKey"]).Returns((string?)null);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new NasaApodService(new HttpClient(), _mockConfig.Object));
        }

        private HttpClient SetupMockHttpClient(NasaApodResponse? response, HttpStatusCode statusCode)
        {
            _mockHttpHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = statusCode,
                    Content = response != null
                        ? new StringContent(JsonSerializer.Serialize(response))
                        : new StringContent(string.Empty)
                });

            return new HttpClient(_mockHttpHandler.Object);
        }
    }
}
