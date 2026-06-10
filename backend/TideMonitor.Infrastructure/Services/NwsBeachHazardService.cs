using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using TideMonitor.Core.Interfaces;
using TideMonitor.Core.Models;

namespace TideMonitor.Infrastructure.Services;

public class NwsBeachHazardService : IBeachHazardService
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<NwsBeachHazardService> _logger;

    private const string AlertsUrl = "https://api.weather.gov/alerts/active";

    // Beach/ocean-related event types from NWS
    private static readonly HashSet<string> BeachHazardEvents = new(StringComparer.OrdinalIgnoreCase)
    {
        "Beach Hazards Statement",
        "Rip Current Statement",
        "Rip Current Warning",
        "High Surf Warning",
        "High Surf Advisory",
        "Coastal Flood Warning",
        "Coastal Flood Advisory",
        "Coastal Flood Statement",
        "Lakeshore Flood Warning",
        "Lakeshore Flood Advisory",
        "Lakeshore Flood Statement",
        "Hurricane Warning",
        "Hurricane Watch",
        "Tropical Storm Warning",
        "Tropical Storm Watch",
        "Storm Surge Warning",
        "Storm Surge Watch",
        "Severe Thunderstorm Warning",
        "Tornado Warning",
    };

    public NwsBeachHazardService(HttpClient httpClient, IMemoryCache cache, ILogger<NwsBeachHazardService> logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _logger = logger;
    }

    public async Task<List<BeachHazard>> GetBeachHazardsAsync(double latitude, double longitude, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"beach_hazards_{latitude:F4}_{longitude:F4}";

        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);

            try
            {
                // NWS API: get active alerts for a point
                var url = $"{AlertsUrl}?point={latitude},{longitude}";
                
                // NWS requires a User-Agent header
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.UserAgent.ParseAdd("(TideMonitor, contact@tidemonitor.local)");

                var response = await _httpClient.SendAsync(request, cancellationToken);
                response.EnsureSuccessStatusCode();

                var alertsResponse = await response.Content.ReadFromJsonAsync<NwsAlertsResponse>(cancellationToken: cancellationToken);

                if (alertsResponse?.Features == null || alertsResponse.Features.Count == 0)
                {
                    _logger.LogInformation("No active alerts found for location {Lat},{Lng}", latitude, longitude);
                    return new List<BeachHazard>();
                }

                var hazards = alertsResponse.Features
                    .Select(f => f.Properties)
                    .Where(p => p != null && IsBeachHazardEvent(p.Event))
                    .Select(p => new BeachHazard
                    {
                        Id = p!.Id ?? string.Empty,
                        Headline = p.Headline ?? string.Empty,
                        Description = p.Description ?? string.Empty,
                        Severity = p.Severity ?? "Unknown",
                        EventType = p.Event ?? "Unknown",
                        Effective = ParseNwsDate(p.Effective),
                        Expires = ParseNwsDate(p.Expires),
                        Instruction = p.Instruction ?? string.Empty,
                        Area = p.Area ?? string.Empty,
                    })
                    .ToList();

                _logger.LogInformation("Found {Count} beach hazard alerts for location {Lat},{Lng}", hazards.Count, latitude, longitude);
                return hazards;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch beach hazards from NWS API for location {Lat},{Lng}", latitude, longitude);
                return new List<BeachHazard>();
            }
        }) ?? new List<BeachHazard>();
    }

    private static bool IsBeachHazardEvent(string? eventType)
    {
        if (string.IsNullOrWhiteSpace(eventType))
            return false;

        return BeachHazardEvents.Contains(eventType);
    }

    private static DateTime? ParseNwsDate(string? dateStr)
    {
        if (string.IsNullOrWhiteSpace(dateStr))
            return null;

        if (DateTime.TryParse(dateStr, out var date))
            return date;

        return null;
    }

    // NWS API response models
    private class NwsAlertsResponse
    {
        [JsonPropertyName("features")]
        public List<NwsAlertFeature>? Features { get; set; }
    }

    private class NwsAlertFeature
    {
        [JsonPropertyName("properties")]
        public NwsAlertProperties? Properties { get; set; }
    }

    private class NwsAlertProperties
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("headline")]
        public string? Headline { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("severity")]
        public string? Severity { get; set; }

        [JsonPropertyName("event")]
        public string? Event { get; set; }

        [JsonPropertyName("effective")]
        public string? Effective { get; set; }

        [JsonPropertyName("expires")]
        public string? Expires { get; set; }

        [JsonPropertyName("instruction")]
        public string? Instruction { get; set; }

        [JsonPropertyName("areaDesc")]
        public string? Area { get; set; }
    }
}
