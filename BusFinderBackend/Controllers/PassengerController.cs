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
using BusFinderBackend.DTOs.Passenger;

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
        private readonly IHubContext<PassengerHub> _passengerHubContext;

        public PassengerController(
            PassengerService passengerService,
            IConfiguration configuration,
            EmailService emailService,
            ILogger<PassengerController> logger,
            IHubContext<PassengerHub> passengerHubContext)
        {
            _passengerService = passengerService;
            _configuration = configuration;
            _emailService = emailService;
            _logger = logger;
            _passengerHubContext = passengerHubContext;
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
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
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
        [SwaggerOperation(Summary = "Update passenger password.")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> UpdatePassword([FromBody] UpdatePasswordRequestDto request)
        {
            var result = await _passengerService.UpdatePasswordAsync(request.Email, request.OldPassword, request.NewPassword);
            if (!result.Success)
            {
                return BadRequest(new { error = result.ErrorCode, message = result.ErrorMessage });
            }
            return Ok(new { message = "Password updated successfully." });
        }

        [HttpPost("forgot-password")]
        [SwaggerOperation(Summary = "Send passenger password reset link.")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDto request)
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
        [SwaggerOperation(Summary = "Reset passenger password.")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDto request)
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
        [SwaggerOperation(Summary = "Sign in a passenger with Google.")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> GoogleSignIn([FromBody] GoogleSignInRequestDto request)
        {
            var result = await _passengerService.GoogleSignInAsync(request.IdToken);
            if (!result.Success)
            {
                return Unauthorized(new { error = result.ErrorCode, message = result.ErrorMessage });
            }
            return Ok(new { message = "Google Sign-In successful." });
        }

        [HttpPost("{passengerId}/favorite-routes")]
        [SwaggerOperation(Summary = "Add a favorite route for a passenger.")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> AddFavoriteRoute(string passengerId, [FromBody] FavoriteRouteRequestDto request)
        {
            await _passengerService.AddFavoriteRouteAsync(passengerId, request.RouteNumber);
            return Ok(new { message = "Favorite route added." });
        }

        [HttpDelete("{passengerId}/favorite-routes/{routeId}")]
        [SwaggerOperation(Summary = "Remove a favorite route for a passenger.")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
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
        [SwaggerOperation(Summary = "Add a favorite place for a passenger.")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> AddFavoritePlace(string passengerId, [FromBody] FavoritePlaceRequestDto request)
        {
            if (string.IsNullOrEmpty(request.PlaceId))
            {
                return BadRequest(new { error = "placeId is required." });
            }

            await _passengerService.AddFavoritePlaceAsync(passengerId, request.PlaceId);
            return Ok(new { message = "Favorite place added." });
        }

        [HttpDelete("{passengerId}/favorite-places/{placeId}")]
        [SwaggerOperation(Summary = "Remove a favorite place for a passenger.")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
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
        [SwaggerOperation(Summary = "Update the current location of a passenger.")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> UpdateLocation(string passengerId, [FromBody] PassengerLocationUpdateRequestDto request)
        {
            await _passengerService.UpdateLocationAsync(passengerId, request.Latitude, request.Longitude);
            // Send the CORRECT SignalR message with actual coordinates
            await _passengerHubContext.Clients.All.SendAsync("PassengerLocationUpdated", 
                passengerId, 
                request.Latitude, 
                request.Longitude);
            return Ok(new { message = "Location updated." });
        }

        [HttpPost("verify-oob-code")]
        [SwaggerOperation(Summary = "Verify passenger password reset OOB code.")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> VerifyOobCode([FromBody] PassengerVerifyOobCodeRequestDto request)
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
        [SwaggerOperation(Summary = "Upload a profile picture for a passenger.")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
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
        [SwaggerOperation(Summary = "Get the profile picture of a passenger.")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
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
        [SwaggerOperation(Summary = "Update the profile picture of a passenger.")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
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
        [SwaggerOperation(Summary = "Get passenger ID by email.")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
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
    }
} 