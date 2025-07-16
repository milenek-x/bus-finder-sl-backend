using Microsoft.AspNetCore.Mvc;
using BusFinderBackend.Services;
using Microsoft.Extensions.Logging;
using BusFinderBackend.Model;
using Swashbuckle.AspNetCore.Annotations;

namespace BusFinderBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MapController : ControllerBase
    {
        private readonly MapService _mapService;
        private readonly ILogger<MapController> _logger;

        public MapController(MapService mapService, ILogger<MapController> logger)
        {
            _mapService = mapService;
            _logger = logger;
        }

        [HttpGet("map-configuration")]
        [SwaggerOperation(Summary = "Get all map configuration options.")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public IActionResult GetAllMapConfiguration()
        {
            var config = _mapService.GetAllMapConfiguration();
            return Ok(config);
        }

        [HttpGet("admin-view-all-bus")]
        [SwaggerOperation(Summary = "Get admin view configuration for all buses.")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public IActionResult GetAdminViewAllBusConfiguration()
        {
            var config = _mapService.GetAdminViewAllBusConfiguration();
            return Ok(config);
        }

        [HttpGet("staff-view-live-bus-shift")]
        [SwaggerOperation(Summary = "Get staff view configuration for live bus shift.")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public IActionResult GetStaffViewLiveBusShiftConfiguration([FromQuery] string? busRoute = null)
        {
            var config = _mapService.GetStaffViewLiveBusShiftConfiguration(busRoute);
            return Ok(config);
        }

        [HttpGet("passenger-view-live-bus-route")]
        [SwaggerOperation(Summary = "Get passenger view configuration for live bus route.")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public IActionResult GetPassengerViewLiveBusRouteConfiguration([FromQuery] string? busRoute = null)
        {
            var config = _mapService.GetPassengerViewLiveBusRouteConfiguration(busRoute);
            return Ok(config);
        }

        [HttpGet("passenger-view-live-location")]
        [SwaggerOperation(Summary = "Get passenger view configuration for live location only.")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public IActionResult GetPassengerViewLiveLocationConfiguration()
        {
            var config = _mapService.GetPassengerViewLiveLocation();
            return Ok(config);
        }
    }
}