using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using TideMonitor.Core.Interfaces;
using TideMonitor.Core.Models;

namespace TideMonitor.Infrastructure.Services;

public class NominatimGeocodingService : ILocationService
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<NominatimGeocodingService> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private const string NominatimUrl = "https://nominatim.openstreetmap.org/search";

    public NominatimGeocodingService(HttpClient httpClient, IMemoryCache cache, ILogger<NominatimGeocodingService> logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _logger = logger;
    }

    public async Task<Location?> GeocodeAddressAsync(string address, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"geocode_{address.ToLowerInvariant().Replace(" ", "_")}";

        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(7);

            var url = $"{NominatimUrl}?q={Uri.EscapeDataString(address)}&format=json&limit=1&addressdetails=1";

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            // Nominatim requires a User-Agent header
            request.Headers.UserAgent.ParseAdd("TideMonitor/1.0 (tide-monitor-app)");

            try
            {
                var response = await _httpClient.SendAsync(request, cancellationToken);
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var results = JsonSerializer.Deserialize<List<NominatimResult>>(content, JsonOptions);

                if (results == null || results.Count == 0)
                {
                    _logger.LogWarning("No geocoding results found for address: {Address}", address);
                    return null;
                }

                var result = results[0];
                return new Location
                {
                    Latitude = double.Parse(result.Latitude, System.Globalization.CultureInfo.InvariantCulture),
                    Longitude = double.Parse(result.Longitude, System.Globalization.CultureInfo.InvariantCulture),
                    DisplayName = result.DisplayName
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Geocoding failed for address: {Address}", address);
                return null;
            }
        });
    }

    private class NominatimResult
    {
        [JsonPropertyName("lat")]
        public string Latitude { get; set; } = string.Empty;

        [JsonPropertyName("lon")]
        public string Longitude { get; set; } = string.Empty;

        [JsonPropertyName("display_name")]
        public string DisplayName { get; set; } = string.Empty;
    }
}
