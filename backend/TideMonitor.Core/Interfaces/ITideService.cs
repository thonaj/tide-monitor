using TideMonitor.Core.Models;

namespace TideMonitor.Core.Interfaces;

public interface ITideService
{
    Task<TideData> GetTideDataAsync(double latitude, double longitude, CancellationToken cancellationToken = default);
}
