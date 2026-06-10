using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using TideMonitor.Core.Interfaces;
using TideMonitor.Core.Models;

namespace TideMonitor.Infrastructure.Services;

public class NoaaTideService : ITideService
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<NoaaTideService> _logger;

    private const string StationsUrl = "https://api.tidesandcurrents.noaa.gov/mdapi/prod/webapi/stations.json";
    private const string DataUrl = "https://api.tidesandcurrents.noaa.gov/api/prod/datagetter";

    // Danger level thresholds (in feet MLLW)
    // These are relative to the station's typical tide range
    private const double CautionThresholdPercentile = 0.75; // Top 25% of range
    private const double WarningThresholdPercentile = 0.90; // Top 10% of range
    private const double DangerThresholdPercentile = 0.95;  // Top 5% of range

    public NoaaTideService(HttpClient httpClient, IMemoryCache cache, ILogger<NoaaTideService> logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _logger = logger;
    }

    public async Task<TideData> GetTideDataAsync(double latitude, double longitude, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"tide_{latitude:F4}_{longitude:F4}";

        var result = await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15);

            // 1. Find nearest NOAA station
            var station = await FindNearestStationAsync(latitude, longitude, cancellationToken);
            if (station == null)
                throw new InvalidOperationException("No NOAA tide station found near the specified location.");

            _logger.LogInformation("Using NOAA station: {Id} - {Name}", station.Id, station.Name);

            // 2. Fetch tide predictions (48 hours)
            var now = DateTime.UtcNow;
            var stationTz = GetStationTimeZone(station.Id);
            var nowStationLocal = TimeZoneInfo.ConvertTimeFromUtc(now, stationTz);
            var beginDate = nowStationLocal.AddHours(-6).ToString("yyyyMMdd HH:mm");
            var endDate = nowStationLocal.AddHours(48).ToString("yyyyMMdd HH:mm");

            var predictions = await FetchTidePredictionsAsync(station.Id, beginDate, endDate, cancellationToken);

            // 3. Fetch current water level (with fallback to predictions)
            var currentLevel = await FetchCurrentWaterLevelAsync(station.Id, predictions, cancellationToken);

            // 4. Fetch water temperature (with Open-Meteo backup)
            var waterTemp = await FetchWaterTemperatureWithFallbackAsync(station.Id, latitude, longitude, cancellationToken);

            // 5. Determine tide status using local time (predictions are in LST/LDT)
            var nowLocal = TimeZoneInfo.ConvertTimeFromUtc(now, GetStationTimeZone(station.Id));
            _logger.LogInformation("Current UTC: {Utc}, Local: {Local}, Predictions count: {Count}, First: {First}, Last: {Last}",
                now, nowLocal, predictions.Count,
                predictions.Count > 0 ? predictions[0].Time.ToString("yyyy-MM-dd HH:mm") : "none",
                predictions.Count > 0 ? predictions[^1].Time.ToString("yyyy-MM-dd HH:mm") : "none");
            var status = DetermineTideStatus(currentLevel, predictions, nowLocal);
            _logger.LogInformation("Tide status determined: {Status} (level: {Level})", status, currentLevel);

            // 6. Calculate danger level
            var dangerLevel = CalculateDangerLevel(currentLevel, predictions);

            return new TideData
            {
                Location = new Location
                {
                    Latitude = latitude,
                    Longitude = longitude,
                    DisplayName = station.Name
                },
                Timestamp = now,
                CurrentWaterLevel = currentLevel,
                CurrentStatus = status,
                WaterTemperature = waterTemp,
                DangerLevel = dangerLevel,
                Predictions = predictions
            };
        });

        return result ?? throw new InvalidOperationException("Failed to retrieve tide data from cache.");

    }

    /// <summary>
    /// Gets the time zone for a NOAA station based on its longitude.
    /// NOAA predictions use LST/LDT (local standard/daylight time).
    /// </summary>
    private static TimeZoneInfo GetStationTimeZone(string stationId)
    {
        // NOAA stations in the US use Eastern time by default.
        // For a more robust solution, we'd look up the station's time zone.
        // Since most NOAA tide stations are on the US coasts, use Eastern Time.
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
        }
        catch
        {
            return TimeZoneInfo.Local;
        }
    }

    private async Task<NoaaStation?> FindNearestStationAsync(double latitude, double longitude, CancellationToken ct)
    {
        var stations = await _cache.GetOrCreateAsync("noaa_stations", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(1);

            var response = await _httpClient.GetFromJsonAsync<NoaaStationResponse>(
                $"{StationsUrl}?type=tidepredictions", ct);

            return response?.Stations ?? [];
        });

        if (stations == null || stations.Count == 0)
            return null;

        return stations
            .Select(s => new
            {
                Station = s,
                Distance = HaversineDistance(latitude, longitude, s.Latitude, s.Longitude)
            })
            .OrderBy(s => s.Distance)
            .First()
            .Station;
    }

    private async Task<List<TidePrediction>> FetchTidePredictionsAsync(
        string stationId, string beginDate, string endDate, CancellationToken ct)
    {
        var url = $"{DataUrl}?station={stationId}&product=predictions&datum=MLLW&units=english&time_zone=lst_ldt&format=json&begin_date={beginDate}&end_date={endDate}&interval=hilo";

        try
        {
            var response = await _httpClient.GetFromJsonAsync<NoaaPredictionResponse>(url, ct);

            return response?.Predictions?.Select(p => new TidePrediction
            {
                // Parse as local time (NOAA returns LST/LDT without timezone info)
                Time = DateTime.SpecifyKind(DateTime.Parse(p.Time), DateTimeKind.Local),
                WaterLevel = double.Parse(p.Value),
                Event = p.Type switch
                {
                    "H" => TideEvent.HighTide,
                    "L" => TideEvent.LowTide,
                    _ => null
                }
            }).ToList() ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch tide predictions, falling back to 6-hour interval");

            // Fallback: use 6-hour interval data
            url = $"{DataUrl}?station={stationId}&product=predictions&datum=MLLW&units=english&time_zone=lst_ldt&format=json&begin_date={beginDate}&end_date={endDate}&interval=6";

            var response = await _httpClient.GetFromJsonAsync<NoaaPredictionResponse>(url, ct);

            return response?.Predictions?.Select(p => new TidePrediction
            {
                Time = DateTime.Parse(p.Time),
                WaterLevel = double.Parse(p.Value)
            }).ToList() ?? [];
        }
    }

    private async Task<double> FetchCurrentWaterLevelAsync(string stationId, List<TidePrediction> predictions, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var beginDate = now.AddHours(-1).ToString("yyyyMMdd HH:mm");
        var endDate = now.AddHours(1).ToString("yyyyMMdd HH:mm");

        var url = $"{DataUrl}?station={stationId}&product=water_level&datum=MLLW&units=english&time_zone=lst_ldt&format=json&begin_date={beginDate}&end_date={endDate}";

        try
        {
            var response = await _httpClient.GetFromJsonAsync<NoaaWaterLevelResponse>(url, ct);
            var data = response?.Data;
            if (data != null && data.Count > 0)
            {
                return double.Parse(data[^1].Value); // Most recent reading
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch current water level from water_level product");
        }

        // Fallback 1: use the most recent 6-minute prediction
        try
        {
            var predUrl = $"{DataUrl}?station={stationId}&product=predictions&datum=MLLW&units=english&time_zone=lst_ldt&format=json&begin_date={beginDate}&end_date={endDate}";
            var predResponse = await _httpClient.GetFromJsonAsync<NoaaPredictionResponse>(predUrl, ct);
            var pred = predResponse?.Predictions?.LastOrDefault();
            if (pred != null)
            {
                return double.Parse(pred.Value);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch current water level from predictions fallback");
        }

        // Fallback 2: interpolate from the hilo predictions we already have
        if (predictions.Count >= 2)
        {
            var nowLocal = TimeZoneInfo.ConvertTimeFromUtc(now, GetStationTimeZone(stationId));
            var before = predictions.LastOrDefault(p => p.Time <= nowLocal);
            var after = predictions.FirstOrDefault(p => p.Time >= nowLocal);

            if (before != null && after != null && after.Time != before.Time)
            {
                var totalMinutes = (after.Time - before.Time).TotalMinutes;
                var elapsedMinutes = (nowLocal - before.Time).TotalMinutes;
                var ratio = elapsedMinutes / totalMinutes;
                var interpolated = before.WaterLevel + (after.WaterLevel - before.WaterLevel) * ratio;
                _logger.LogInformation("Interpolated current water level from predictions: {Level}", interpolated);
                return interpolated;
            }
        }

        // Fallback 3: return the most recent prediction value
        if (predictions.Count > 0)
        {
            return predictions[^1].WaterLevel;
        }

        return 0;
    }

    private async Task<double?> FetchWaterTemperatureWithFallbackAsync(string stationId, double latitude, double longitude, CancellationToken ct)
    {
        // Try NOAA water_temperature product first
        var temp = await FetchNoaaWaterTemperatureAsync(stationId, ct);
        if (temp.HasValue)
            return temp.Value;

        // Fallback: try Open-Meteo Marine API (free, no API key required)
        _logger.LogInformation("NOAA water temperature not available, trying Open-Meteo API");
        try
        {
            var openMeteoUrl = $"https://marine-api.open-meteo.com/v1/marine?latitude={latitude}&longitude={longitude}&current=sea_surface_temperature&temperature_unit=fahrenheit";
            var response = await _httpClient.GetFromJsonAsync<OpenMeteoMarineResponse>(openMeteoUrl, ct);
            if (response?.Current?.SeaSurfaceTemperature.HasValue == true)
            {
                _logger.LogInformation("Got water temperature from Open-Meteo: {Temp}°F", response.Current.SeaSurfaceTemperature.Value);
                return response.Current.SeaSurfaceTemperature.Value;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch water temperature from Open-Meteo");
        }

        return null;
    }

    private async Task<double?> FetchNoaaWaterTemperatureAsync(string stationId, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var beginDate = now.AddHours(-24).ToString("yyyyMMdd HH:mm");
        var endDate = now.ToString("yyyyMMdd HH:mm");

        var url = $"{DataUrl}?station={stationId}&product=water_temperature&units=english&time_zone=lst_ldt&format=json&begin_date={beginDate}&end_date={endDate}";

        try
        {
            var response = await _httpClient.GetFromJsonAsync<NoaaWaterTemperatureResponse>(url, ct);
            var data = response?.Data;
            if (data != null && data.Count > 0)
            {
                // Return the most recent temperature reading
                var latest = data[^1];
                return double.Parse(latest.Value);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch water temperature from NOAA (may not be available at this station)");
        }

        return null;
    }


    private static DangerLevel CalculateDangerLevel(double currentLevel, List<TidePrediction> predictions)
    {
        if (predictions.Count == 0)
            return DangerLevel.Safe;

        // Calculate the typical tide range from predictions
        var allLevels = predictions.Select(p => p.WaterLevel).ToList();
        allLevels.Add(currentLevel);
        allLevels.Sort();

        var count = allLevels.Count;
        var cautionIdx = (int)(count * CautionThresholdPercentile);
        var warningIdx = (int)(count * WarningThresholdPercentile);
        var dangerIdx = (int)(count * DangerThresholdPercentile);

        var cautionLevel = allLevels[Math.Clamp(cautionIdx, 0, count - 1)];
        var warningLevel = allLevels[Math.Clamp(warningIdx, 0, count - 1)];
        var dangerLevel = allLevels[Math.Clamp(dangerIdx, 0, count - 1)];

        // Also check against absolute thresholds (MLLW feet)
        // These provide a safety net when percentile-based thresholds are too lenient
        const double absoluteCaution = 2.0;   // 2 ft - above normal high tide
        const double absoluteWarning = 4.0;   // 4 ft - significant flooding risk
        const double absoluteDanger = 6.0;    // 6 ft - extreme flooding

        // Use the more conservative (lower) threshold between percentile and absolute
        var effectiveCaution = Math.Min(cautionLevel, absoluteCaution);
        var effectiveWarning = Math.Min(warningLevel, absoluteWarning);
        var effectiveDanger = Math.Min(dangerLevel, absoluteDanger);

        if (currentLevel >= effectiveDanger)
            return DangerLevel.Danger;
        if (currentLevel >= effectiveWarning)
            return DangerLevel.Warning;
        if (currentLevel >= effectiveCaution)
            return DangerLevel.Caution;

        return DangerLevel.Safe;
    }


    private static TideStatus DetermineTideStatus(double currentLevel, List<TidePrediction> predictions, DateTime nowLocal)
    {
        if (predictions.Count < 2)
            return TideStatus.Rising;

        // Find the most recent prediction before now and the next one after now
        var before = predictions.LastOrDefault(p => p.Time <= nowLocal);
        var after = predictions.FirstOrDefault(p => p.Time >= nowLocal);

        // Check if we're near a high or low tide event (within 90 minutes)
        var nearEvent = predictions
            .Where(p => p.Event.HasValue && Math.Abs((p.Time - nowLocal).TotalMinutes) <= 90)
            .OrderBy(p => Math.Abs((p.Time - nowLocal).TotalMinutes))
            .FirstOrDefault();

        if (nearEvent != null)
        {
            // If we're within 5 minutes of the event, call it exactly High/Low
            if (Math.Abs((nearEvent.Time - nowLocal).TotalMinutes) <= 5)
            {
                return nearEvent.Event == TideEvent.HighTide ? TideStatus.High : TideStatus.Low;
            }

            // If the event is in the past (we're past high tide), we should be falling
            if (nearEvent.Time < nowLocal && nearEvent.Event == TideEvent.HighTide)
                return TideStatus.Falling;

            // If the event is in the past (we're past low tide), we should be rising
            if (nearEvent.Time < nowLocal && nearEvent.Event == TideEvent.LowTide)
                return TideStatus.Rising;

            // If the event is in the future (approaching high tide), we should be rising
            if (nearEvent.Time > nowLocal && nearEvent.Event == TideEvent.HighTide)
                return TideStatus.Rising;

            // If the event is in the future (approaching low tide), we should be falling
            if (nearEvent.Time > nowLocal && nearEvent.Event == TideEvent.LowTide)
                return TideStatus.Falling;
        }

        // Fallback: use before/after predictions to determine trend
        if (before != null && after != null)
        {
            return before.WaterLevel < after.WaterLevel ? TideStatus.Rising : TideStatus.Falling;
        }

        // If we only have predictions after now, check the first two
        if (before == null && after != null)
        {
            var afterIdx = predictions.IndexOf(after);
            if (afterIdx < predictions.Count - 1)
            {
                var nextAfter = predictions[afterIdx + 1];
                return after.WaterLevel < nextAfter.WaterLevel ? TideStatus.Rising : TideStatus.Falling;
            }
        }

        return TideStatus.Rising;
    }

    private static double HaversineDistance(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 3958.8; // Earth radius in miles
        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180;

    // NOAA API response models
    private class NoaaStationResponse
    {
        [JsonPropertyName("stations")]
        public List<NoaaStation> Stations { get; set; } = [];
    }

    private class NoaaStation
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("lat")]
        public double Latitude { get; set; }

        [JsonPropertyName("lng")]
        public double Longitude { get; set; }
    }

    private class NoaaPredictionResponse
    {
        [JsonPropertyName("predictions")]
        public List<NoaaPrediction>? Predictions { get; set; }
    }

    private class NoaaPrediction
    {
        [JsonPropertyName("t")]
        public string Time { get; set; } = string.Empty;

        [JsonPropertyName("v")]
        public string Value { get; set; } = string.Empty;

        [JsonPropertyName("type")]
        public string? Type { get; set; }
    }

    private class NoaaWaterLevelResponse
    {
        [JsonPropertyName("data")]
        public List<NoaaWaterLevel>? Data { get; set; }
    }

    private class NoaaWaterLevel
    {
        [JsonPropertyName("t")]
        public string Time { get; set; } = string.Empty;

        [JsonPropertyName("v")]
        public string Value { get; set; } = string.Empty;

        [JsonPropertyName("s")]
        public string? Sigma { get; set; }

        [JsonPropertyName("f")]
        public string? Flags { get; set; }

        [JsonPropertyName("q")]
        public string? Quality { get; set; }
    }

    private class NoaaWaterTemperatureResponse
    {
        [JsonPropertyName("data")]
        public List<NoaaWaterTemperature>? Data { get; set; }
    }

    private class NoaaWaterTemperature
    {
        [JsonPropertyName("t")]
        public string Time { get; set; } = string.Empty;

        [JsonPropertyName("v")]
        public string Value { get; set; } = string.Empty;

        [JsonPropertyName("f")]
        public string? Flags { get; set; }

        [JsonPropertyName("q")]
        public string? Quality { get; set; }
    }

    // Open-Meteo Marine API response models
    private class OpenMeteoMarineResponse
    {
        [JsonPropertyName("current")]
        public OpenMeteoCurrent? Current { get; set; }
    }

    private class OpenMeteoCurrent
    {
        [JsonPropertyName("sea_surface_temperature")]
        public double? SeaSurfaceTemperature { get; set; }
    }
}
