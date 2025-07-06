using BusFinderBackend.Model;
using BusFinderBackend.Services;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace BusFinderBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BusController : ControllerBase
    {
        private readonly BusService _busService;

        public BusController(BusService busService)
        {
            _busService = busService;
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
            await _busService.UpdateCurrentLocationAsync(numberPlate, request.CurrentLocationLatitude, request.CurrentLocationLongitude);
            return NoContent();
        }

        [HttpPut("{numberPlate}")]
        public async Task<IActionResult> UpdateBus(string numberPlate, [FromBody] Bus bus)
        {
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