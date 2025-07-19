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
using Swashbuckle.AspNetCore.Annotations;
using BusFinderBackend.DTOs.Admin;

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
        [SwaggerOperation(Summary = "Get all admins.")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<List<Admin>>> GetAllAdmins()
        {
            var admins = await _adminService.GetAllAdminsAsync();
            return Ok(admins);
        }

        [HttpGet("{id}")]
        [SwaggerOperation(Summary = "Get an admin by ID.")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<Admin>> GetAdminById(string id)
        {
            var admin = await _adminService.GetAdminByIdAsync(id);
            if (admin == null)
                return NotFound();
            return Ok(admin);
        }

        [HttpPost]
        [SwaggerOperation(Summary = "Add a new admin.")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
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
        [SwaggerOperation(Summary = "Update an admin by ID.")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
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

        [HttpPut("{adminId}/avatar")]
        [SwaggerOperation(Summary = "Update the avatar for an admin.")]
        public async Task<IActionResult> UpdateAvatar(string adminId, [FromBody] int avatarId)
        {
            await _adminService.UpdateAvatarAsync(adminId, avatarId);
            return Ok(new { message = "Avatar updated successfully." });
        }

        [HttpDelete("{id}")]
        [SwaggerOperation(Summary = "Delete an admin by ID.")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult> DeleteAdmin(string id)
        {
            var existing = await _adminService.GetAdminByIdAsync(id);
            if (existing == null)
                return NotFound();

            await _adminService.DeleteAdminAsync(id);
            return NoContent();
        }

        [HttpPost("login")]
        [SwaggerOperation(Summary = "Login an admin.")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> Login([FromBody] AdminLoginRequestDto request)
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
        [SwaggerOperation(Summary = "Update admin password.")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> UpdatePassword([FromBody] AdminPasswordUpdateRequestDto request)
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

        [HttpPost("forgot-password")]
        [SwaggerOperation(Summary = "Send admin password reset link.")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> ForgotPassword([FromBody] AdminForgotPasswordRequestDto request)
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
            catch (FirebaseAdmin.Auth.FirebaseAuthException ex)
            {
                _logger.LogError(ex, "Error generating password reset link for email: {Email}", request.Email);
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("reset-password")]
        [SwaggerOperation(Summary = "Reset admin password.")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> ResetPassword([FromBody] AdminResetPasswordRequestDto request)
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

        [HttpPost("verify-oob-code")]
        [SwaggerOperation(Summary = "Verify admin password reset OOB code.")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> VerifyOobCode([FromBody] AdminVerifyOobCodeRequestDto request)
        {
            if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.OobCode))
            {
                return BadRequest(new { error = "Email and oobCode are required." });
            }

            bool isValid = await _adminService.VerifyOobCodeAsync(request.Email, request.OobCode);
            if (isValid)
            {
                return Ok(new { message = "OobCode is valid." });
            }
            return BadRequest(new { error = "Invalid or expired oobCode." });
        }

        [HttpGet("get-id-by-email/{email}")]
        [SwaggerOperation(Summary = "Get admin ID by email.")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetAdminIdByEmail(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return BadRequest("Email cannot be null or empty.");
            }

            var adminId = await _adminService.GetAdminIdByEmailAsync(email);
            if (adminId == null)
            {
                return NotFound(new { error = "Admin not found." });
            }

            return Ok(new { adminId });
        }
    }
}
