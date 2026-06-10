using TideMonitor.Core.Models;

namespace TideMonitor.Core.Interfaces;

public interface ILocationService
{
    Task<Location?> GeocodeAddressAsync(string address, CancellationToken cancellationToken = default);
}
