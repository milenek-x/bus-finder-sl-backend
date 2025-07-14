using BusFinderBackend.Model;
using BusFinderBackend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.Json;
using BusFinderBackend.DTOs.BusRoute;
//
using Swashbuckle.AspNetCore.Annotations;

namespace BusFinderBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BusRouteController : ControllerBase
    {
        private readonly BusRouteService _busRouteService;
        private readonly IConfiguration _configuration;

        public BusRouteController(BusRouteService busRouteService, IConfiguration configuration)
        {
            _busRouteService = busRouteService;
            _configuration = configuration;
        }

        [HttpGet]
        [SwaggerOperation(Summary = "Get all bus routes.")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<List<BusRoute>>> GetAllBusRoutes()
        {
            var busRoutes = await _busRouteService.GetAllBusRoutesAsync();
            return Ok(busRoutes);
        }

        [HttpGet("{routeNumber}")]
        [SwaggerOperation(Summary = "Get a bus route by its route number.")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<BusRoute>> GetBusRouteByNumber(string routeNumber)
        {
            var busRoute = await _busRouteService.GetBusRouteByNumberAsync(routeNumber);
            if (busRoute == null)
                return NotFound();
            return Ok(busRoute);
        }

        [HttpPost]
        [SwaggerOperation(Summary = "Add a new bus route.")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> AddBusRoute([FromBody] BusRoute busRoute)
        {
            var result = await _busRouteService.AddBusRouteAsync(busRoute);
            if (!result.Success)
            {
                return BadRequest(new
                {
                    error = result.ErrorCode,
                    message = result.ErrorMessage
                });
            }
            return CreatedAtAction(nameof(GetBusRouteByNumber), new { routeNumber = busRoute.RouteNumber }, busRoute);
        }

        [HttpPut("{routeNumber}")]
        [SwaggerOperation(Summary = "Update an existing bus route by route number.")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult> UpdateBusRoute(string routeNumber, [FromBody] BusRoute busRoute)
        {
            var existing = await _busRouteService.GetBusRouteByNumberAsync(routeNumber);
            if (existing == null)
                return NotFound();

            await _busRouteService.UpdateBusRouteAsync(routeNumber, busRoute);
            return NoContent();
        }

        [HttpDelete("{routeNumber}")]
        [SwaggerOperation(Summary = "Delete a bus route by route number.")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult> DeleteBusRoute(string routeNumber)
        {
            var existing = await _busRouteService.GetBusRouteByNumberAsync(routeNumber);
            if (existing == null)
                return NotFound();

            await _busRouteService.DeleteBusRouteAsync(routeNumber);
            return NoContent();
        }

        [HttpGet("geojson")]
        [SwaggerOperation(Summary = "Get all bus routes as GeoJSON.")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<string>> GetGeoJSONBusRoutes()
        {
            var geoJson = await _busRouteService.GetGeoJSONBusRoutesAsync();
            return Ok(geoJson);
        }

        [HttpGet("single/geojson/{routeNumber}")]
        [SwaggerOperation(Summary = "Get a single bus route as GeoJSON by route number.")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<string>> GetGeoJSONBusRoute(string routeNumber)
        {
            var geoJson = await _busRouteService.GetGeoJSONBusRouteAsync(routeNumber);
            if (geoJson == null)
                return NotFound();
            return Ok(geoJson);
        }

        [HttpGet("by-stops")]
        [SwaggerOperation(Summary = "Get bus routes by starting and ending stops, date, and time.")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<List<BusRouteWithShiftsDto>>> GetBusRoutesByStops(string startingPoint, string endingPoint, string date, string time)
        {
            var routes = await _busRouteService.GetBusRoutesByStopsAsync(startingPoint, endingPoint, date, time);
            if (routes == null || routes.Count == 0)
                return NotFound();
            return Ok(routes);
        }

        [HttpPost("calculate-distance")]
        [SwaggerOperation(Summary = "Calculate the total road distance for a bus route by connecting all stops in order.")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<double>> CalculateRouteDistance([FromBody] List<string> routeStops)
        {
            var distance = await _busRouteService.CalculateRouteDistanceAsync(routeStops);
            if (distance == null)
                return BadRequest(new { error = "Unable to calculate distance. Check if all stops exist and are valid." });
            return Ok(distance);
        }
    }
}
