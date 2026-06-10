namespace TideMonitor.Core.Models;

public class BeachHazard
{
    public string Id { get; set; } = string.Empty;
    public string Headline { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty; // Extreme, Severe, Moderate, Minor
    public string EventType { get; set; } = string.Empty; // e.g., "Beach Hazards Statement", "Rip Current Statement", "High Surf Warning"
    public DateTime? Effective { get; set; }
    public DateTime? Expires { get; set; }
    public string Instruction { get; set; } = string.Empty;
    public string Area { get; set; } = string.Empty;
}

public enum HazardSeverity
{
    Minor,
    Moderate,
    Severe,
    Extreme
}
