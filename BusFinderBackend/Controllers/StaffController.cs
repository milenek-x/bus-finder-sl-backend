using BusFinderBackend.Model;
using BusFinderBackend.Services;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BusFinderBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StaffController : ControllerBase
    {
        private readonly StaffService _staffService;
        private readonly IConfiguration _configuration;
        private readonly EmailService _emailService;
        private readonly ILogger<StaffController> _logger;

        public StaffController(
            StaffService staffService,
            IConfiguration configuration,
            EmailService emailService,
            ILogger<StaffController> logger)
        {
            _staffService = staffService;
            _configuration = configuration;
            _emailService = emailService;
            _logger = logger;
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

        public class StaffLoginRequest
        {
            public string? Email { get; set; }
            public string? Password { get; set; }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] StaffLoginRequest request)
        {
            var result = await _staffService.LoginAsync(request.Email!, request.Password!);
            if (!result.Success)
            {
                return Unauthorized(new
                {
                    error = result.ErrorCode,
                    message = result.ErrorMessage
                });
            }
            return Ok(new { message = "Login successful." });
        }

        public class StaffPasswordUpdateRequest
        {
            public string? Email { get; set; }
            public string? OldPassword { get; set; }
            public string? NewPassword { get; set; }
        }

        [HttpPost("update-password")]
        public async Task<IActionResult> UpdatePassword([FromBody] StaffPasswordUpdateRequest request)
        {
            var result = await _staffService.UpdatePasswordAsync(request.Email!, request.OldPassword!, request.NewPassword!);
            if (!result.Success)
            {
                return BadRequest(new
                {
                    error = result.ErrorCode,
                    message = result.ErrorMessage
                });
            }
            return Ok(new { message = "Password updated successfully." });
        }

        public class ForgotPasswordRequest
        {
            public string Email { get; set; } = string.Empty;
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            try
            {
                string resetLink = await _staffService.GeneratePasswordResetLinkAsync(request.Email);
                return Ok(new { resetLink });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        public class ResetPasswordRequest
        {
            public string Email { get; set; } = string.Empty;
            public string NewPassword { get; set; } = string.Empty;
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            try
            {
                bool result = await _staffService.ResetPasswordAsync(request.Email, request.NewPassword);
                return Ok(new { message = "Password reset successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}
