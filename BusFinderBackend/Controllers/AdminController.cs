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

        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateAdmin(string id, [FromBody] Admin admin)
        {
            var existing = await _adminService.GetAdminByIdAsync(id);
            if (existing == null)
                return NotFound();

            await _adminService.UpdateAdminAsync(id, admin);
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

            var loginResult = await Firebase.FirebaseAuthHelper.LoginWithEmailPasswordAsync(apiKey, request.Email!, request.OldPassword!);
            if (!loginResult.Success)
            {
                return Unauthorized(new
                {
                    error = loginResult.ErrorCode,
                    message = "Invalid email or password."
                });
            }

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
    }
}
