using BusFinderBackend.Model;
using BusFinderBackend.Services;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using BusFinderBackend.DTOs.BusShift;
using Swashbuckle.AspNetCore.Annotations;

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
        [SwaggerOperation(Summary = "Get all bus shifts.")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<List<BusShift>>> GetAllBusShifts()
        {
            var busShifts = await _busShiftService.GetAllBusShiftsAsync();
            return Ok(busShifts);
        }

        [HttpGet("{shiftId}")]
        [SwaggerOperation(Summary = "Get a bus shift by its ID.")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<BusShift?>> GetBusShiftById(string shiftId)
        {
            var busShift = await _busShiftService.GetBusShiftByIdAsync(shiftId);
            if (busShift == null)
                return NotFound();
            return Ok(busShift);
        }

        [HttpPost]
        [SwaggerOperation(Summary = "Add a new bus shift.")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> AddBusShift([FromBody] BusShift busShift)
        {
            await _busShiftService.AddBusShiftAsync(busShift);
            return CreatedAtAction(nameof(GetBusShiftById), new { shiftId = busShift.ShiftId }, busShift);
        }

        [HttpPut("{shiftId}")]
        [SwaggerOperation(Summary = "Update a bus shift by its ID.")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> UpdateBusShift(string shiftId, [FromBody] BusShift busShift)
        {
            await _busShiftService.UpdateBusShiftAsync(shiftId, busShift);
            return NoContent();
        }

        [HttpDelete("{shiftId}")]
        [SwaggerOperation(Summary = "Delete a bus shift by its ID.")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> DeleteBusShift(string shiftId)
        {
            await _busShiftService.DeleteBusShiftAsync(shiftId);
            return NoContent();
        }

        [HttpDelete("{shiftId}/normal")]
        [SwaggerOperation(Summary = "Remove the normal shift details from a bus shift.")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> RemoveNormalShift(string shiftId)
        {
            await _busShiftService.RemoveNormalShiftAsync(shiftId);
            return NoContent();
        }

        [HttpDelete("{shiftId}/reverse")]
        [SwaggerOperation(Summary = "Remove the reverse shift details from a bus shift.")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> RemoveReverseShift(string shiftId)
        {
            await _busShiftService.RemoveReverseShiftAsync(shiftId);
            return NoContent();
        }

        [HttpGet("by-route/{routeNumber}/future")]
        [SwaggerOperation(Summary = "Get future bus shifts by route number, date, and time.")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<List<BusShiftDto>>> GetFutureBusShiftsByRouteNumber(string routeNumber, string date, string time)

        {
            var shifts = await _busShiftService.GetBusShiftsByRouteNumberAsync(routeNumber, date, time);
            if (shifts == null || shifts.Count == 0)
                return NotFound();
            return Ok(shifts);
        }

        [HttpPost("notify-shift-start")]
        [SwaggerOperation(Summary = "Notify all clients that a staff member has started their shift.")]
        public async Task<IActionResult> NotifyShiftStart([FromBody] string staffId)
        {
            await _busShiftService.NotifyShiftStartAsync(staffId);
            return Ok(new { message = "Shift start notification sent." });
        }

        [HttpPost("notify-shift-end")]
        [SwaggerOperation(Summary = "Notify all clients that a staff member has ended their shift.")]
        public async Task<IActionResult> NotifyShiftEnd([FromBody] string staffId)
        {
            await _busShiftService.NotifyShiftEndAsync(staffId);
            return Ok(new { message = "Shift end notification sent." });
        }

        [HttpPost("notify-shift-interval")]
        [SwaggerOperation(Summary = "Notify all clients that a staff member is on a shift interval.")]
        public async Task<IActionResult> NotifyShiftInterval([FromBody] string staffId)
        {
            await _busShiftService.NotifyShiftIntervalAsync(staffId);
            return Ok(new { message = "Shift interval notification sent." });
        }
    }
} 