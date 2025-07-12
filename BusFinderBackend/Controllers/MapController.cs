using Microsoft.AspNetCore.Mvc;
using BusFinderBackend.Services;
using Microsoft.Extensions.Logging;

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
        public IActionResult GetAllMapConfiguration()
        {
            var config = _mapService.GetAllMapConfiguration();
            return Ok(config);
        }

        [HttpGet("admin-view-all-bus")] // Endpoint for admin view all bus configuration
        public IActionResult GetAdminViewAllBusConfiguration()
        {
            var config = _mapService.GetAdminViewAllBusConfiguration();
            return Ok(config);
        }

        [HttpGet("staff-view-live-bus-shift")] // Endpoint for staff view live bus shift configuration
        public IActionResult GetStaffViewLiveBusShiftConfiguration()
        {
            var config = _mapService.GetStaffViewLiveBusShiftConfiguration();
            return Ok(config);
        }

        [HttpGet("passenger-view-live-bus-route")] // Endpoint for passenger view live bus route configuration
        public IActionResult GetPassengerViewLiveBusRouteConfiguration()
        {
            var config = _mapService.GetPassengerViewLiveBusRouteConfiguration();
            return Ok(config);
        }
    }
}