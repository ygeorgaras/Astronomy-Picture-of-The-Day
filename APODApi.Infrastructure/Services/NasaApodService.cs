using APODApi.Core.Interfaces;
using APODApi.Core.Models;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;
using static System.Runtime.InteropServices.JavaScript.JSType;


namespace APODApi.Infrastructure.Services
{
    public class NasaApodService : INasaApodService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public NasaApodService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _apiKey = configuration["NASA:ApiKey"]
                ?? throw new ArgumentNullException("NASA:ApiKey configuration is missing");
            _httpClient.BaseAddress = new Uri("https://api.nasa.gov");
        }

        public async Task<NasaApodResponse> GetLatestApodAsync()
        {
            var response = await _httpClient.GetAsync($"/planetary/apod?api_key={_apiKey}");
            response.EnsureSuccessStatusCode();

            var apodResponse = await response.Content.ReadFromJsonAsync<NasaApodResponse>();

            return apodResponse
                ?? throw new InvalidOperationException("Failed to deserialize NASA APOD response");
        }

        public async Task<NasaApodResponse> GetApodByDateAsync(string date)
        {
            var response = await _httpClient.GetAsync($"/planetary/apod?api_key={_apiKey}&date={date}");
            response.EnsureSuccessStatusCode();

            var apodResponse = await response.Content.ReadFromJsonAsync<NasaApodResponse>();

            return apodResponse
                ?? throw new InvalidOperationException("Failed to deserialize NASA APOD response");
        }

    }
}
