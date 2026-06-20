using CirculationService.DTOs.External;
using System.Net.Http.Json;

namespace CirculationService.Services;

public class CatalogClient
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public CatalogClient(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task<BookAvailabilityResponse?> GetBookAvailabilityAsync(Guid bookId)
    {
        var apiKey = _configuration["InternalService:ApiKey"];

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"/api/internal/books/{bookId}/availability");

        request.Headers.Add("X-Internal-Service-Key", apiKey);

        var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<BookAvailabilityResponse>();
    }

    public async Task<bool> DecreaseAvailableCopiesAsync(Guid bookId)
    {
        var apiKey = _configuration["InternalService:ApiKey"];

        using var request = new HttpRequestMessage(
            HttpMethod.Put,
            $"/api/internal/books/{bookId}/decrease");

        request.Headers.Add("X-Internal-Service-Key", apiKey);

        var response = await _httpClient.SendAsync(request);

        return response.IsSuccessStatusCode;
    }

    public async Task<bool> IncreaseAvailableCopiesAsync(Guid bookId)
    {
        var apiKey = _configuration["InternalService:ApiKey"];

        using var request = new HttpRequestMessage(
            HttpMethod.Put,
            $"/api/internal/books/{bookId}/increase");

        request.Headers.Add("X-Internal-Service-Key", apiKey);

        var response = await _httpClient.SendAsync(request);

        return response.IsSuccessStatusCode;
    }
}