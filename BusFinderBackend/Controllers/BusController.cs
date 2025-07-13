using BusFinderBackend.Model;
using BusFinderBackend.Services;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using Microsoft.AspNetCore.SignalR;
using BusFinderBackend.Hubs;
using System.Text.Json;

namespace BusFinderBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BusController : ControllerBase
    {
        private readonly BusService _busService;

        private readonly IHubContext<BusHub> _hubContext;

        public BusController(BusService busService, IHubContext<BusHub> hubContext)
        {
            _busService = busService;
            _hubContext = hubContext;
        }

        [HttpGet]
        public async Task<ActionResult<List<Bus>>> GetAllBuses()
        {
            var buses = await _busService.GetAllBusesAsync();
            return Ok(buses);
        }

        [HttpGet("{numberPlate}")]
        public async Task<ActionResult<Bus?>> GetBusByNumberPlate(string numberPlate)
        {
            var bus = await _busService.GetBusByNumberPlateAsync(numberPlate);
            if (bus == null)
                return NotFound();
            return Ok(bus);
        }

        [HttpPost]
        public async Task<IActionResult> AddBus([FromBody] Bus bus)
        {
            // Validate DriverId and ConductorId
            if (string.IsNullOrEmpty(bus.DriverId))
            {
                return BadRequest("DriverId cannot be null or empty.");
            }

            if (string.IsNullOrEmpty(bus.ConductorId))
            {
                return BadRequest("ConductorId cannot be null or empty.");
            }

            await _busService.AddBusAsync(bus);
            return CreatedAtAction(nameof(GetBusByNumberPlate), new { numberPlate = bus.NumberPlate }, bus);
        }

        [HttpPut("{numberPlate}/capacity")]
        public async Task<IActionResult> UpdateBusCapacity(string numberPlate, [FromBody] BusCapacityUpdateRequest request)
        {
            await _busService.UpdateBusCapacityAsync(numberPlate, request.BusCapacity);
            return NoContent();
        }

        [HttpPut("{numberPlate}/sos-status")]
        public async Task<IActionResult> UpdateSosStatus(string numberPlate, [FromBody] SosStatusUpdateRequest request)
        {
            await _busService.UpdateSosStatusAsync(numberPlate, request.SosStatus);
            return NoContent();
        }

        [HttpPut("{numberPlate}/location")]
        public async Task<IActionResult> UpdateCurrentLocation(string numberPlate, [FromBody] LocationUpdateRequest request)
        {
            // Update the database first
            await _busService.UpdateCurrentLocationAsync(numberPlate, request.CurrentLocationLatitude, request.CurrentLocationLongitude);

            // Send the CORRECT SignalR message with actual coordinates
            await _hubContext.Clients.All.SendAsync("BusLocationUpdated", 
                numberPlate, 
                request.CurrentLocationLatitude, 
                request.CurrentLocationLongitude);

            return NoContent();
        }

        [HttpPut("{numberPlate}")]
        public async Task<IActionResult> UpdateBus(string numberPlate, [FromBody] Bus bus)
        {
            // Validate DriverId and ConductorId
            if (string.IsNullOrEmpty(bus.DriverId))
            {
                return BadRequest("DriverId cannot be null or empty.");
            }

            if (string.IsNullOrEmpty(bus.ConductorId))
            {
                return BadRequest("ConductorId cannot be null or empty.");
            }

            // Additional business logic can be added here if needed
            await _busService.UpdateBusAsync(numberPlate, bus);
            return NoContent();
        }

        [HttpDelete("{numberPlate}")]
        public async Task<IActionResult> DeleteBus(string numberPlate)
        {
            await _busService.DeleteBusAsync(numberPlate);
            return NoContent();
        }

        [HttpPut("{numberPlate}/update-location-if-needed")]
        public async Task<IActionResult> UpdateLocationIfNeeded(string numberPlate)
        {
            try
            {
                await _busService.UpdateBusLocationIfNeededAsync(numberPlate);
                return NoContent(); // Return 204 No Content if successful
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to update location.", message = ex.Message });
            }
        }

        [HttpGet("geojson")]
        public async Task<IActionResult> GetGeoJSONBuses()
        {
            var geoJson = await _busService.GetGeoJSONBusesAsync();
            return Ok(geoJson);
        }

        [HttpGet("single/geojson/{numberPlate}")]
        public async Task<IActionResult> GetSingleBusGeoJSON(string numberPlate)
        {
            var geoJson = await _busService.GetSingleBusGeoJSONAsync(numberPlate);
            if (geoJson == null)
                return NotFound();
            return Ok(geoJson);
        }
    }

    public class BusCapacityUpdateRequest
    {
        public bool BusCapacity { get; set; }
    }

    public class SosStatusUpdateRequest
    {
        public bool SosStatus { get; set; }
    }

    public class LocationUpdateRequest
    {
        public double CurrentLocationLatitude { get; set; }
        public double CurrentLocationLongitude { get; set; }
    }
} 