using BusFinderBackend.Model;
using BusFinderBackend.Services;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using FirebaseAdmin.Auth;
using Microsoft.AspNetCore.Http;
using System.IO;
using BusFinderBackend.Hubs;
using Microsoft.AspNetCore.SignalR;
using Swashbuckle.AspNetCore.Annotations;

namespace BusFinderBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PassengerController : ControllerBase
    {
        private readonly PassengerService _passengerService;
        private readonly IConfiguration _configuration;
        private readonly EmailService _emailService;
        private readonly ILogger<PassengerController> _logger;
        private readonly IHubContext<BusHub> _hubContext;

        public PassengerController(
            PassengerService passengerService,
            IConfiguration configuration,
            EmailService emailService,
            ILogger<PassengerController> logger,
            IHubContext<BusHub> hubContext)
        {
            _passengerService = passengerService;
            _configuration = configuration;
            _emailService = emailService;
            _logger = logger;
            _hubContext = hubContext;
        }

        [HttpGet]
        [SwaggerOperation(Summary = "Get all passengers.")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<List<Passenger>>> GetAllPassengers()
        {
            var passengers = await _passengerService.GetAllPassengersAsync();
            return Ok(passengers);
        }

        [HttpGet("{id}")]
        [SwaggerOperation(Summary = "Get a passenger by ID.")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<Passenger>> GetPassengerById(string id)
        {
            var passenger = await _passengerService.GetPassengerByIdAsync(id);
            if (passenger == null)
                return NotFound();
            return Ok(passenger);
        }

        [HttpPost]
        [SwaggerOperation(Summary = "Add a new passenger.")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> AddPassenger([FromBody] Passenger passenger)
        {
            if (string.IsNullOrEmpty(passenger.ProfileImageUrl))
            {
                return BadRequest("Profile image URL must be provided.");
            }

            var result = await _passengerService.AddPassengerAsync(passenger);
            if (!result.Success)
            {
                return BadRequest(new
                {
                    error = result.ErrorCode,
                    message = result.ErrorMessage
                });
            }
            return CreatedAtAction(nameof(GetPassengerById), new { id = passenger.PassengerId }, passenger);
        }

        [HttpPut("{id}")]
        [SwaggerOperation(Summary = "Update a passenger by ID.")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult> UpdatePassenger(string id, [FromBody] Passenger passenger)
        {
            var existing = await _passengerService.GetPassengerByIdAsync(id);
            if (existing == null)
                return NotFound();

            await _passengerService.UpdatePassengerAsync(id, passenger);
            return NoContent();
        }

        [HttpDelete("{id}")]
        [SwaggerOperation(Summary = "Delete a passenger by ID.")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult> DeletePassenger(string id)
        {
            await _passengerService.DeletePassengerAsync(id);
            return NoContent();
        }

        [HttpPost("login")]
        [SwaggerOperation(Summary = "Login a passenger.")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
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
                return Unauthorized(new { error = result.ErrorCode, message = result.ErrorMessage });
            }

            return Ok(new { token = result.IdToken, refreshToken = result.RefreshToken });
        }

        [HttpPost("update-password")]
        public async Task<IActionResult> UpdatePassword([FromBody] UpdatePasswordRequest request)
        {
            var result = await _passengerService.UpdatePasswordAsync(request.Email, request.OldPassword, request.NewPassword);
            if (!result.Success)
            {
                return BadRequest(new { error = result.ErrorCode, message = result.ErrorMessage });
            }
            return Ok(new { message = "Password updated successfully." });
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            try
            {
                string resetLink = await _passengerService.GeneratePasswordResetLinkAsync(request.Email);
                return Ok(new { resetLink });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            try
            {
                bool result = await _passengerService.ResetPasswordAsync(request.Email, request.NewPassword);
                return Ok(new { message = "Password reset successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("google-signin")]
        public async Task<IActionResult> GoogleSignIn([FromBody] GoogleSignInRequest request)
        {
            var result = await _passengerService.GoogleSignInAsync(request.IdToken);
            if (!result.Success)
            {
                return Unauthorized(new { error = result.ErrorCode, message = result.ErrorMessage });
            }
            return Ok(new { message = "Google Sign-In successful." });
        }

        [HttpPost("{passengerId}/favorite-routes")]
        public async Task<IActionResult> AddFavoriteRoute(string passengerId, [FromBody] FavoriteRouteRequest request)
        {
            await _passengerService.AddFavoriteRouteAsync(passengerId, request.RouteNumber);
            return Ok(new { message = "Favorite route added." });
        }

        [HttpDelete("{passengerId}/favorite-routes/{routeId}")]
        public async Task<IActionResult> RemoveFavoriteRoute(string passengerId, string routeId)
        {
            try
            {
                await _passengerService.RemoveFavoriteRouteAsync(passengerId, routeId);
                return Ok(new { message = "Favorite route removed." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("{passengerId}/favorite-places")]
        public async Task<IActionResult> AddFavoritePlace(string passengerId, [FromBody] FavoritePlaceRequest request)
        {
            if (string.IsNullOrEmpty(request.PlaceId))
            {
                return BadRequest(new { error = "placeId is required." });
            }

            await _passengerService.AddFavoritePlaceAsync(passengerId, request.PlaceId);
            return Ok(new { message = "Favorite place added." });
        }

        [HttpDelete("{passengerId}/favorite-places/{placeId}")]
        public async Task<IActionResult> RemoveFavoritePlace(string passengerId, string placeId)
        {
            try
            {
                await _passengerService.RemoveFavoritePlaceAsync(passengerId, placeId);
                return Ok(new { message = "Favorite place removed." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPut("{passengerId}/location")]
        public async Task<IActionResult> UpdateLocation(string passengerId, [FromBody] PassengerLocationUpdateRequest request)
        {
            await _passengerService.UpdateLocationAsync(passengerId, request.Latitude, request.Longitude);
            // Send the CORRECT SignalR message with actual coordinates
            await _hubContext.Clients.All.SendAsync("PassengerLocationUpdated", 
                passengerId, 
                request.Latitude, 
                request.Longitude);
            return Ok(new { message = "Location updated." });
        }

        [HttpPost("verify-oob-code")]
        public async Task<IActionResult> VerifyOobCode([FromBody] PassengerVerifyOobCodeRequest request)
        {
            if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.OobCode))
            {
                return BadRequest(new { error = "Email and oobCode are required." });
            }

            bool isValid = await _passengerService.VerifyOobCodeAsync(request.Email, request.OobCode);
            if (isValid)
            {
                return Ok(new { message = "OobCode is valid." });
            }
            return BadRequest(new { error = "Invalid or expired oobCode." });
        }

        [HttpPost("upload-profile-picture")]
        public async Task<IActionResult> UploadProfilePicture( IFormFile file)
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
                    var link = await _passengerService.UploadProfilePictureAsync(stream, fileName);
                    return Ok(new { link });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading profile picture.");
                return StatusCode(500, new { error = "Failed to upload profile picture.", message = ex.Message });
            }
        }

        [HttpGet("profile-picture/{passengerId}")]
        public async Task<IActionResult> GetProfilePicture(string passengerId)
        {
            var passenger = await _passengerService.GetPassengerByIdAsync(passengerId);
            if (passenger == null || string.IsNullOrEmpty(passenger.ProfileImageUrl))
            {
                return NotFound(new { error = "Admin not found or profile picture not set." });
            }

            var imageBytes = await _passengerService.GetProfilePictureAsync(passenger.ProfileImageUrl);
            return File(imageBytes, "image/jpeg"); // Return the image as a file response
        }

        [HttpPut("update-profile-picture/{passengerId}")]
        public async Task<IActionResult> UpdateProfilePicture(string passengerId, IFormFile file)
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
                    var fileName = $"profile_picture_{passengerId}_{DateTime.UtcNow.Ticks}.jpg";
                    var link = await _passengerService.UpdateProfilePictureAsync(passengerId, stream, fileName);
                    return Ok(new { link });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating profile picture for passenger {PassengerId}.", passengerId);
                return StatusCode(500, new { error = "Failed to update profile picture.", message = ex.Message });
            }
        }

        [HttpGet("get-id-by-email/{email}")]
        public async Task<IActionResult> GetPassengerIdByEmail(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return BadRequest("Email cannot be null or empty.");
            }

            var passengerId = await _passengerService.GetPassengerIdByEmailAsync(email);
            if (passengerId == null)
            {
                return NotFound(new { error = "Passenger not found." });
            }

            return Ok(new { passengerId });
        }

        public class LoginRequest
        {
            public string Email { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
        }

        public class UpdatePasswordRequest
        {
            public string Email { get; set; } = string.Empty;
            public string OldPassword { get; set; } = string.Empty;
            public string NewPassword { get; set; } = string.Empty;
        }

        public class ForgotPasswordRequest
        {
            public string Email { get; set; } = string.Empty;
        }

        public class ResetPasswordRequest
        {
            public string Email { get; set; } = string.Empty;
            public string NewPassword { get; set; } = string.Empty;
        }

        public class GoogleSignInRequest
        {
            public string IdToken { get; set; } = string.Empty; // The ID token received from Google Sign-In
        }

        public class PassengerLocationUpdateRequest
        {
            public double? Latitude { get; set; }
            public double? Longitude { get; set; }
        }

        public class FavoriteRouteRequest
        {
            public string RouteNumber { get; set; } = string.Empty; // Accept routeNumber as a string
        }

        public class FavoritePlaceRequest
        {
            public string PlaceId { get; set; } = string.Empty; // Accept placeId as a string
        }

        public class PassengerVerifyOobCodeRequest
        {
            public string? Email { get; set; }
            public string? OobCode { get; set; }
        }

        public class PassengerForgotPasswordRequest
        {
            public string? Email { get; set; }
            public string? NewPassword { get; set; }
        }

        public class PassengerResetPasswordRequest
        {
            public string? Email { get; set; }
            public string? NewPassword { get; set; }
        }
    }
} 