using TideMonitor.Core.Models;

namespace TideMonitor.Core.Interfaces;

public interface IBeachHazardService
{
    /// <summary>
    /// Gets active beach hazard alerts (rip currents, high surf, etc.) from the NWS API for a location.
    /// </summary>
    Task<List<BeachHazard>> GetBeachHazardsAsync(double latitude, double longitude, CancellationToken cancellationToken = default);
}
