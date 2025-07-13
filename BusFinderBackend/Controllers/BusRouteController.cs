using BusFinderBackend.Model;
using BusFinderBackend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.Json;
using BusFinderBackend.Model.DTOs;

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
        public async Task<ActionResult<List<BusRoute>>> GetAllBusRoutes()
        {
            var busRoutes = await _busRouteService.GetAllBusRoutesAsync();
            return Ok(busRoutes);
        }

        [HttpGet("{routeNumber}")]
        public async Task<ActionResult<BusRoute>> GetBusRouteByNumber(string routeNumber)
        {
            var busRoute = await _busRouteService.GetBusRouteByNumberAsync(routeNumber);
            if (busRoute == null)
                return NotFound();
            return Ok(busRoute);
        }

        [HttpPost]
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
        public async Task<ActionResult> UpdateBusRoute(string routeNumber, [FromBody] BusRoute busRoute)
        {
            var existing = await _busRouteService.GetBusRouteByNumberAsync(routeNumber);
            if (existing == null)
                return NotFound();

            await _busRouteService.UpdateBusRouteAsync(routeNumber, busRoute);
            return NoContent();
        }

        [HttpDelete("{routeNumber}")]
        public async Task<ActionResult> DeleteBusRoute(string routeNumber)
        {
            var existing = await _busRouteService.GetBusRouteByNumberAsync(routeNumber);
            if (existing == null)
                return NotFound();

            await _busRouteService.DeleteBusRouteAsync(routeNumber);
            return NoContent();
        }

        [HttpGet("geojson")]
        public async Task<ActionResult<string>> GetGeoJSONBusRoutes()
        {
            var geoJson = await _busRouteService.GetGeoJSONBusRoutesAsync();
            return Ok(geoJson);
        }

        [HttpGet("single/geojson/{routeNumber}")]
        public async Task<ActionResult<string>> GetGeoJSONBusRoute(string routeNumber)
        {
            var geoJson = await _busRouteService.GetGeoJSONBusRouteAsync(routeNumber);
            if (geoJson == null)
                return NotFound();
            return Ok(geoJson);
        }

        [HttpGet("by-stops")]
        public async Task<ActionResult<List<BusRouteWithShiftsDto>>> GetBusRoutesByStops(string startingPoint, string endingPoint, string date, string time)
        {
            var routes = await _busRouteService.GetBusRoutesByStopsAsync(startingPoint, endingPoint, date, time);
            if (routes == null || routes.Count == 0)
                return NotFound();
            return Ok(routes);
        }
    }
}
