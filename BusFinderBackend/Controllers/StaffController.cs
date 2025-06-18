using BusFinderBackend.Model;
using BusFinderBackend.Services;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BusFinderBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StaffController : ControllerBase
    {
        private readonly StaffService _staffService;

        public StaffController(StaffService staffService)
        {
            _staffService = staffService;
        }

        [HttpGet]
        public async Task<ActionResult<List<Staff>>> GetAllStaff()
        {
            var staffList = await _staffService.GetAllStaffAsync();
            return Ok(staffList);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Staff>> GetStaffById(string id)
        {
            var staff = await _staffService.GetStaffByIdAsync(id);
            if (staff == null)
                return NotFound();
            return Ok(staff);
        }

        [HttpPost]
        public async Task<IActionResult> AddStaff([FromBody] Staff staff)
        {
            var result = await _staffService.AddStaffAsync(staff);
            if (!result.Success)
            {
                return BadRequest(new
                {
                    error = result.ErrorCode,
                    message = result.ErrorMessage
                });
            }
            return CreatedAtAction(nameof(GetStaffById), new { id = staff.StaffId }, staff);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateStaff(string id, [FromBody] Staff staff)
        {
            var existing = await _staffService.GetStaffByIdAsync(id);
            if (existing == null)
                return NotFound();

            await _staffService.UpdateStaffAsync(id, staff);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteStaff(string id)
        {
            var existing = await _staffService.GetStaffByIdAsync(id);
            if (existing == null)
                return NotFound();

            await _staffService.DeleteStaffAsync(id);
            return NoContent();
        }
    }
}
