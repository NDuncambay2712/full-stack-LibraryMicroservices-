using CirculationService.DTOs.External;
using System.Net.Http.Json;

namespace CirculationService.Services;

public class IdentityReportClient
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public IdentityReportClient(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task<ReaderStatusResponse?> GetReaderStatusAsync(Guid readerId)
    {
        var apiKey = _configuration["InternalService:ApiKey"];

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"/api/internal/readers/{readerId}/status");

        request.Headers.Add("X-Internal-Service-Key", apiKey);

        var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<ReaderStatusResponse>();
    }

    public async Task<bool> SendBookBorrowedEventAsync(BookBorrowedEventRequest body)
    {
        var apiKey = _configuration["InternalService:ApiKey"];

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/reports/events/book-borrowed");

        request.Headers.Add("X-Internal-Service-Key", apiKey);
        request.Content = JsonContent.Create(body);

        var response = await _httpClient.SendAsync(request);

        return response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.Conflict;
    }

    public async Task<bool> SendBookReturnedEventAsync(BookReturnedEventRequest body)
    {
        var apiKey = _configuration["InternalService:ApiKey"];

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/reports/events/book-returned");

        request.Headers.Add("X-Internal-Service-Key", apiKey);
        request.Content = JsonContent.Create(body);

        var response = await _httpClient.SendAsync(request);

        return response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.Conflict;
    }
}