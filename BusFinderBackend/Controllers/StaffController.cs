using BusFinderBackend.Model;
using BusFinderBackend.Services;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using System.IO;
using Swashbuckle.AspNetCore.Annotations;
using BusFinderBackend.DTOs.Staff;

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
        [SwaggerOperation(Summary = "Get all staff members.")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<List<Staff>>> GetAllStaff()
        {
            var staffList = await _staffService.GetAllStaffAsync();
            return Ok(staffList);
        }

        [HttpGet("{id}")]
        [SwaggerOperation(Summary = "Get a staff member by ID.")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<Staff>> GetStaffById(string id)
        {
            var staff = await _staffService.GetStaffByIdAsync(id);
            if (staff == null)
                return NotFound();
            return Ok(staff);
        }

        [HttpPost]
        [SwaggerOperation(Summary = "Add a new staff member.")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> AddStaff([FromBody] Staff staff)
        {
            _logger.LogInformation("AddStaff endpoint called with: {@Staff}", staff);
            
            var result = await _staffService.AddStaffAsync(staff);
            _logger.LogInformation("AddStaff result: {Result}", result);
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
        [SwaggerOperation(Summary = "Update a staff member by ID.")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult> UpdateStaff(string id, [FromBody] Staff staff)
        {
            // Ensure the password is not included in the update
            var existing = await _staffService.GetStaffByIdAsync(id);
            if (existing == null)
                return NotFound();

            await _staffService.UpdateStaffAsync(id, staff);
            return NoContent();
        }

        [HttpDelete("{id}")]
        [SwaggerOperation(Summary = "Delete a staff member by ID.")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult> DeleteStaff(string id)
        {
            var existing = await _staffService.GetStaffByIdAsync(id);
            if (existing == null)
                return NotFound();

            await _staffService.DeleteStaffAsync(id);
            return NoContent();
        }

        [HttpGet("role/{role}")]
        [SwaggerOperation(Summary = "Get staff members by role.")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<List<Staff>>> GetStaffByRole(string role)
        {
            var staffList = await _staffService.GetStaffByRoleAsync(role);
            return Ok(staffList);
        }

        [HttpPost("login")]
        [SwaggerOperation(Summary = "Login a staff member.")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> Login([FromBody] StaffLoginRequestDto request)
        {
            var firebaseSection = _configuration.GetSection("Firebase");
            var apiKey = firebaseSection["ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
            {
                return StatusCode(500, new { error = "NO_API_KEY", message = "Firebase API key is not configured." });
            }

            var result = await Firebase.FirebaseAuthHelper.LoginWithEmailPasswordAsync(apiKey, request.Email!, request.Password!);

            if (!result.Success)
            {
                return Unauthorized(new
                {
                    error = result.ErrorCode,
                    message = result.ErrorMessage
                });
            }

            return Ok(new
            {
                token = result.IdToken,
                refreshToken = result.RefreshToken
            });
        }

        [HttpPost("update-password")]
        [SwaggerOperation(Summary = "Update staff password.")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> UpdatePassword([FromBody] StaffPasswordUpdateRequestDto request)
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

        [HttpPost("forgot-password")]
        [SwaggerOperation(Summary = "Send staff password reset link.")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> ForgotPassword([FromBody] StaffForgotPasswordRequestDto request)
        {
            try
            {
                string resetLink = await _staffService.GeneratePasswordResetLinkAsync(request.Email!);
                return Ok(new { resetLink });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("reset-password")]
        [SwaggerOperation(Summary = "Reset staff password.")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> ResetPassword([FromBody] StaffResetPasswordRequestDto request)
        {
            try
            {
                bool result = await _staffService.ResetPasswordAsync(request.Email!, request.NewPassword!);
                return Ok(new { message = "Password reset successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        public class StaffVerifyOobCodeRequest
        {
            public string? Email { get; set; }
            public string? OobCode { get; set; }
        }

        [HttpPut("{staffId}/avatar")]
        [SwaggerOperation(Summary = "Update the avatar for a staff member.")]
        public async Task<IActionResult> UpdateAvatar(string staffId, [FromBody] int avatarId)
        {
            await _staffService.UpdateAvatarAsync(staffId, avatarId);
            return Ok(new { message = "Avatar updated successfully." });
        }

        [HttpPost("upload-image")]
        [SwaggerOperation(Summary = "Upload an image for a staff member.")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded.");
            }

            try
            {
                var link = await _staffService.UploadImageAsync(file);
                return Ok(new { link });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading image.");
                return StatusCode(500, new { error = "Failed to upload image.", message = ex.Message });
            }
        }

        [HttpGet("get-id-by-email/{email}")]
        [SwaggerOperation(Summary = "Get staff ID by email.")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetStaffIdByEmail(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return BadRequest("Email cannot be null or empty.");
            }

            var staffId = await _staffService.GetStaffIdByEmailAsync(email);
            if (staffId == null)
            {
                return NotFound(new { error = "Staff not found." });
            }

            return Ok(new { staffId });
        }
    }
}
