using Microsoft.AspNetCore.Mvc;
using TideMonitor.Core.Interfaces;
using TideMonitor.Core.Models;

namespace TideMonitor.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TideController : ControllerBase
{
    private readonly ITideService _tideService;
    private readonly IBeachHazardService _beachHazardService;

    public TideController(ITideService tideService, IBeachHazardService beachHazardService)
    {
        _tideService = tideService;
        _beachHazardService = beachHazardService;
    }

    /// <summary>
    /// Get current tide data, 48-hour predictions, and beach hazard alerts for a location.
    /// </summary>
    /// <param name="lat">Latitude</param>
    /// <param name="lng">Longitude</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpGet]
    public async Task<ActionResult<TideData>> GetTideData(
        [FromQuery] double lat,
        [FromQuery] double lng,
        CancellationToken cancellationToken)
    {
        if (lat < -90 || lat > 90)
            return BadRequest("Latitude must be between -90 and 90.");

        if (lng < -180 || lng > 180)
            return BadRequest("Longitude must be between -180 and 180.");

        try
        {
            // Fetch tide data and beach hazards in parallel
            var tideTask = _tideService.GetTideDataAsync(lat, lng, cancellationToken);
            var hazardsTask = _beachHazardService.GetBeachHazardsAsync(lat, lng, cancellationToken);

            await Task.WhenAll(tideTask, hazardsTask);

            var tideData = tideTask.Result;
            tideData.BeachHazards = hazardsTask.Result;

            return Ok(tideData);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"An error occurred while fetching tide data: {ex.Message}");
        }
    }
}
