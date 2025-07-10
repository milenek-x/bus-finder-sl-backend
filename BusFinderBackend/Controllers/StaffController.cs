using BusFinderBackend.Model;
using BusFinderBackend.Services;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using System.IO;

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
        public async Task<ActionResult> DeleteStaff(string id)
        {
            var existing = await _staffService.GetStaffByIdAsync(id);
            if (existing == null)
                return NotFound();

            await _staffService.DeleteStaffAsync(id);
            return NoContent();
        }

        [HttpGet("role/{role}")]
        public async Task<ActionResult<List<Staff>>> GetStaffByRole(string role)
        {
            var staffList = await _staffService.GetStaffByRoleAsync(role);
            return Ok(staffList);
        }

        public class StaffLoginRequest
        {
            public string? Email { get; set; }
            public string? Password { get; set; }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] StaffLoginRequest request)
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

        public class StaffForgotPasswordRequest
        {
            public string? Email { get; set; }
            public string? NewPassword { get; set; }
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] StaffForgotPasswordRequest request)
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

        public class StaffResetPasswordRequest
        {
            public string? Email { get; set; }
            public string? NewPassword { get; set; }
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] StaffResetPasswordRequest request)
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

        [HttpPost("upload-profile-picture")]
        public async Task<IActionResult> UploadProfilePicture(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded.");
            }

            try
            {
                using (var stream = file.OpenReadStream())
                {
                    var fileName = $"profile_picture_{DateTime.UtcNow.Ticks}.jpg";
                    var link = await _staffService.UploadProfilePictureAsync(stream, fileName);
                    return Ok(new { link });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading profile picture.");
                return StatusCode(500, new { error = "Failed to upload profile picture.", message = ex.Message });
            }
        }

        [HttpGet("{staffId}/profile-picture")]
        public async Task<IActionResult> GetProfilePicture(string staffId)
        {
            var staff = await _staffService.GetStaffByIdAsync(staffId);
            if (staff == null || string.IsNullOrEmpty(staff.ProfilePicture))
            {
                return NotFound(new { error = "Admin not found or profile picture not set." });
            }

            var imageBytes = await _staffService.GetProfilePictureAsync(staff.ProfilePicture);
            return File(imageBytes, "image/jpeg"); // Return the image as a file response
        }

        [HttpPut("{staffId}/update-profile-picture")]
        public async Task<IActionResult> UpdateProfilePicture(string staffId, IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded.");
            }

            try
            {
                using (var stream = new MemoryStream())
                {
                    await file.CopyToAsync(stream);
                    stream.Position = 0;
                    var fileName = $"profile_picture_{staffId}_{DateTime.UtcNow.Ticks}.jpg";
                    var link = await _staffService.UpdateProfilePictureAsync(staffId, stream, fileName);
                    return Ok(new { link });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating profile picture for staff {StaffId}.", staffId);
                return StatusCode(500, new { error = "Failed to update profile picture.", message = ex.Message });
            }
        }

        [HttpPost("upload-image")]
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
