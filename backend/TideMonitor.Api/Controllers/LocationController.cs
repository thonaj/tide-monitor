using Microsoft.AspNetCore.Mvc;
using TideMonitor.Core.Interfaces;
using TideMonitor.Core.Models;

namespace TideMonitor.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LocationController : ControllerBase
{
    private readonly ILocationService _locationService;

    public LocationController(ILocationService locationService)
    {
        _locationService = locationService;
    }

    /// <summary>
    /// Geocode an address to get latitude/longitude coordinates.
    /// </summary>
    /// <param name="address">The address to geocode (e.g., "807 Ocean Drive, Emerald Isle, NC")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpGet("geocode")]
    public async Task<ActionResult<Location>> GeocodeAddress(
        [FromQuery] string address,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(address))
            return BadRequest("Address is required.");

        var location = await _locationService.GeocodeAddressAsync(address, cancellationToken);

        if (location == null)
            return NotFound($"Could not geocode address: {address}");

        return Ok(location);
    }
}
