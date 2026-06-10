namespace TideMonitor.Core.Models;

public class TideData
{
    public Location Location { get; set; } = new();
    public DateTime Timestamp { get; set; }
    public double CurrentWaterLevel { get; set; }
    public TideStatus CurrentStatus { get; set; }
    public double? WaterTemperature { get; set; }
    public DangerLevel DangerLevel { get; set; }
    public List<TidePrediction> Predictions { get; set; } = [];
    public List<BeachHazard> BeachHazards { get; set; } = [];
}

public class TidePrediction
{
    public DateTime Time { get; set; }
    public double WaterLevel { get; set; }
    public TideEvent? Event { get; set; }
}

public enum TideStatus
{
    Rising,
    Falling,
    High,
    Low
}

public enum TideEvent
{
    HighTide,
    LowTide
}

public enum DangerLevel
{
    Safe,
    Caution,
    Warning,
    Danger
}

public class Location
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string DisplayName { get; set; } = string.Empty;
}
