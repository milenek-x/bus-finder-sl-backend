using BusFinderBackend.Model;
using BusFinderBackend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.Json; // Added for JsonSerializer
using System; // Added for Console.WriteLine
using Swashbuckle.AspNetCore.Annotations; // Added for SwaggerOperation

namespace BusFinderBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BusStopController : ControllerBase
    {
        private readonly BusStopService _busStopService;
        private readonly IConfiguration _configuration;

        public BusStopController(BusStopService busStopService, IConfiguration configuration)
        {
            _busStopService = busStopService;
            _configuration = configuration;
        }

        [HttpGet]
        [SwaggerOperation(Summary = "Get all bus stops.")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<List<BusStop>>> GetAllBusStops()
        {
            var busStops = await _busStopService.GetAllBusStopsAsync();
            return Ok(busStops);
        }

        [HttpGet("{name}")]
        [SwaggerOperation(Summary = "Get a bus stop by its name.")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<BusStop>> GetBusStopByName(string name)
        {
            var busStop = await _busStopService.GetBusStopByNameAsync(name);
            if (busStop == null)
                return NotFound();
            return Ok(busStop);
        }

        [HttpPost]
        [SwaggerOperation(Summary = "Add a new bus stop.")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> AddBusStop([FromBody] BusStop busStop)
        {
            var result = await _busStopService.AddBusStopAsync(busStop);
            if (!result.Success)
            {
                return BadRequest(new
                {
                    error = result.ErrorCode,
                    message = result.ErrorMessage
                });
            }
            return CreatedAtAction(nameof(GetBusStopByName), new { name = busStop.StopName }, busStop);
        }

        [HttpPut("{name}")]
        [SwaggerOperation(Summary = "Update a bus stop by its name.")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult> UpdateBusStop(string name, [FromBody] BusStop busStop)
        {
            var existing = await _busStopService.GetBusStopByNameAsync(name);
            if (existing == null)
                return NotFound();

            await _busStopService.UpdateBusStopAsync(name, busStop);
            return NoContent();
        }

        [HttpDelete("{name}")]
        [SwaggerOperation(Summary = "Delete a bus stop by its name.")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult> DeleteBusStop(string name)
        {
            var existing = await _busStopService.GetBusStopByNameAsync(name);
            if (existing == null)
                return NotFound();

            await _busStopService.DeleteBusStopAsync(name);
            return NoContent();
        }

        [HttpGet("search/google/{name}")]
        [SwaggerOperation(Summary = "Search bus stops using Google API by name.")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<List<BusStop>>> SearchUsingGoogleApi(string name)
        {
            var busStops = await _busStopService.SearchBusStopsUsingGoogleApiAsync(name);
            Console.WriteLine(busStops.Count); // Log the busStops for debugging
            if (busStops == null || busStops.Count == 0)
                return NotFound(new { message = "No bus stops found." });
            return Ok(busStops);
        }


        // New endpoint for loose searching
        [HttpGet("search/firebase/{partialName}")]
        [SwaggerOperation(Summary = "Search bus stops by partial name using Firebase.")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<List<BusStop>>> SearchBusStopsByPartialName(string partialName)
        {
            var busStops = await _busStopService.SearchBusStopsByPartialNameAsync(partialName);
            if (busStops == null || busStops.Count == 0)
                return NotFound(new { message = "No bus stops found matching the search criteria." });
            return Ok(busStops);
        }

        [HttpGet("geojson")]
        [SwaggerOperation(Summary = "Get all bus stops as GeoJSON.")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<string>> GetGeoJSONBusStops()
        {
            var geoJson = await _busStopService.GetGeoJSONBusStopsAsync();
            return Ok(geoJson);
        }
    }
}
