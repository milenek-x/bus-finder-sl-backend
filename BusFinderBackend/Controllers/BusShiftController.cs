using BusFinderBackend.Model;
using BusFinderBackend.Services;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BusFinderBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BusShiftController : ControllerBase
    {
        private readonly BusShiftService _busShiftService;

        public BusShiftController(BusShiftService busShiftService)
        {
            _busShiftService = busShiftService;
        }

        [HttpGet]
        public async Task<ActionResult<List<BusShift>>> GetAllBusShifts()
        {
            var busShifts = await _busShiftService.GetAllBusShiftsAsync();
            return Ok(busShifts);
        }

        [HttpGet("{shiftId}")]
        public async Task<ActionResult<BusShift?>> GetBusShiftById(string shiftId)
        {
            var busShift = await _busShiftService.GetBusShiftByIdAsync(shiftId);
            if (busShift == null)
                return NotFound();
            return Ok(busShift);
        }

        [HttpPost]
        public async Task<IActionResult> AddBusShift([FromBody] BusShift busShift)
        {
            await _busShiftService.AddBusShiftAsync(busShift);
            return CreatedAtAction(nameof(GetBusShiftById), new { shiftId = busShift.ShiftId }, busShift);
        }

        [HttpPut("{shiftId}")]
        public async Task<IActionResult> UpdateBusShift(string shiftId, [FromBody] BusShift busShift)
        {
            await _busShiftService.UpdateBusShiftAsync(shiftId, busShift);
            return NoContent();
        }

        [HttpDelete("{shiftId}")]
        public async Task<IActionResult> DeleteBusShift(string shiftId)
        {
            await _busShiftService.DeleteBusShiftAsync(shiftId);
            return NoContent();
        }
    }
} 