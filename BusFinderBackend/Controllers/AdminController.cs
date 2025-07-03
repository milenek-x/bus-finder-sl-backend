using BusFinderBackend.Model;
using BusFinderBackend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using System.Net.Http.Headers;
using System;
using FirebaseAdmin.Auth;
using Microsoft.Extensions.Logging;
using System.IO;

namespace BusFinderBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly AdminService _adminService;
        private readonly IConfiguration _configuration;
        private readonly EmailService _emailService;
        private readonly ILogger<AdminController> _logger;

        public AdminController(AdminService adminService, IConfiguration configuration, EmailService emailService, ILogger<AdminController> logger)
        {
            _adminService = adminService;
            _configuration = configuration;
            _emailService = emailService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<List<Admin>>> GetAllAdmins()
        {
            var admins = await _adminService.GetAllAdminsAsync();
            return Ok(admins);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Admin>> GetAdminById(string id)
        {
            var admin = await _adminService.GetAdminByIdAsync(id);
            if (admin == null)
                return NotFound();
            return Ok(admin);
        }

        [HttpPost]
        public async Task<IActionResult> AddAdmin([FromBody] Admin admin)
        {
            var result = await _adminService.AddAdminAsync(admin);
            if (!result.Success)
            {
                return BadRequest(new
                {
                    error = result.ErrorCode,
                    message = result.ErrorMessage
                });
            }
            admin.AdminId = result.AdminId;
            return CreatedAtAction(nameof(GetAdminById), new { id = admin.AdminId }, admin);
        }

        [HttpPut("{adminId}")]
        public async Task<ActionResult> UpdateAdmin(string adminId, [FromBody] Admin admin)
        {
            // Ensure the password is not included in the update
            admin.Password = null; // Explicitly set to null or ignore this field

            var existing = await _adminService.GetAdminByIdAsync(adminId);
            if (existing == null)
                return NotFound();

            await _adminService.UpdateAdminAsync(adminId, admin);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteAdmin(string id)
        {
            var existing = await _adminService.GetAdminByIdAsync(id);
            if (existing == null)
                return NotFound();

            await _adminService.DeleteAdminAsync(id);
            return NoContent();
        }

        public class AdminLoginRequest
        {
            public string? Email { get; set; }
            public string? Password { get; set; }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] AdminLoginRequest request)
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

        public class AdminPasswordUpdateRequest
        {
            public string? Email { get; set; }
            public string? OldPassword { get; set; }
            public string? NewPassword { get; set; }
        }

        [HttpPost("update-password")]
        public async Task<IActionResult> UpdatePassword([FromBody] AdminPasswordUpdateRequest request)
        {
            var firebaseSection = _configuration.GetSection("Firebase");
            var apiKey = firebaseSection["ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
            {
                return StatusCode(500, new { error = "NO_API_KEY", message = "Firebase API key is not configured." });
            }

            // Authenticate the admin user using the old password
            var loginResult = await Firebase.FirebaseAuthHelper.LoginWithEmailPasswordAsync(apiKey, request.Email!, request.OldPassword!);
            if (!loginResult.Success)
            {
                return Unauthorized(new
                {
                    error = loginResult.ErrorCode,
                    message = "Invalid email or password."
                });
            }

            // Update the password
            var updateResult = await Firebase.FirebaseAuthHelper.UpdatePasswordAsync(apiKey, loginResult.IdToken!, request.NewPassword!);
            if (!updateResult.Success)
            {
                return BadRequest(new
                {
                    error = updateResult.ErrorCode,
                    message = updateResult.ErrorMessage
                });
            }

            return Ok(new { message = "Password updated successfully." });
        }

        public class ForgotPasswordRequest
        {
            public string Email { get; set; } = string.Empty; // Default to empty string
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            if (string.IsNullOrEmpty(request.Email))
            {
                _logger.LogWarning("Forgot password request received with empty email.");
                return BadRequest(new { error = "Email is required." });
            }

            try
            {
                string resetLink = await _adminService.GeneratePasswordResetLinkAsync(request.Email);
                _logger.LogInformation("Password reset link generated for email: {Email}", request.Email);
                return Ok(new { resetLink });
            }
            catch (FirebaseAuthException ex)
            {
                _logger.LogError(ex, "Error generating password reset link for email: {Email}", request.Email);
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.NewPassword))
            {
                return BadRequest(new { error = "Email and new password are required." });
            }

            try
            {
                bool result = await _adminService.ResetPasswordAsync(request.Email, request.NewPassword);
                return Ok(new { message = "Password reset successfully." });
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
                    var fileName = $"profile_picture_{DateTime.UtcNow.Ticks}.jpg"; // Generate a unique file name
                    var link = await _adminService.UploadProfilePictureAsync(stream, fileName);
                    return Ok(new { link }); // Return the link to the uploaded image
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading profile picture.");
                return StatusCode(500, new { error = "Failed to upload profile picture.", message = ex.Message });
            }
        }

        [HttpPost("upload-profile-picture-blob")]
        public async Task<IActionResult> UploadProfilePictureBlob([FromBody] byte[] blob)
        {
            if (blob == null || blob.Length == 0)
            {
                return BadRequest("No file uploaded.");
            }

            using (var stream = new MemoryStream(blob))
            {
                var fileName = $"profile_picture_{DateTime.UtcNow.Ticks}.jpg"; // Generate a unique file name
                var link = await _adminService.UploadProfilePictureAsync(stream, fileName);
                return Ok(new { link }); // Return the link to the uploaded image
            }
        }
    }
}
