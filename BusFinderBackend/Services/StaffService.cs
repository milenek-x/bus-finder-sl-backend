using BusFinderBackend.Repositories;
using BusFinderBackend.Model;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System; // For Exception
using FirebaseAdmin.Auth; // For FirebaseAuth
using System.IO;
using Microsoft.AspNetCore.Http; // For IFormFile

namespace BusFinderBackend.Services
{
    public class StaffService
    {
        private readonly StaffRepository _staffRepository;
        private readonly IConfiguration _configuration;
        private readonly EmailService _emailService;
        private readonly ILogger<StaffService> _logger;
        private readonly DriveImageService _driveImageService;

        public StaffService(
            StaffRepository staffRepository,
            IConfiguration configuration,
            EmailService emailService,
            ILogger<StaffService> logger,
            DriveImageService driveImageService)
        {
            _staffRepository = staffRepository;
            _configuration = configuration;
            _emailService = emailService;
            _logger = logger;
            _driveImageService = driveImageService;
        }

        public Task<List<Staff>> GetAllStaffAsync()
        {
            return _staffRepository.GetAllStaffAsync();
        }

        public Task<Staff?> GetStaffByIdAsync(string staffId)
        {
            return _staffRepository.GetStaffByIdAsync(staffId);
        }

        public async Task<(bool Success, string? ErrorCode, string? ErrorMessage, string? StaffId)> AddStaffAsync(Staff staff)
        {
            var firebaseSection = _configuration.GetSection("Firebase");
            var apiKey = firebaseSection["ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
            {
                return (false, "NO_API_KEY", "Firebase API key is not configured.", null);
            }

            if (string.IsNullOrEmpty(staff.StaffId))
            {
                staff.StaffId = await _staffRepository.GenerateNextStaffIdAsync();
            }

            // Check for null or empty values
            if (string.IsNullOrEmpty(staff.Email) || string.IsNullOrEmpty(staff.Password))
            {
                return (false, "INVALID_INPUT", "Email and password must be provided.", null);
            }

            var firebaseResult = await Firebase.FirebaseAuthHelper.CreateUserAsync(apiKey, staff.Email, staff.Password);

            if (!firebaseResult.Success)
            {
                if (firebaseResult.ErrorCode == "EMAIL_EXISTS")
                {
                    return (false, "EMAIL_EXISTS", "This email is already registered.", null);
                }
                return (false, firebaseResult.ErrorCode, firebaseResult.ErrorMessage, null);
            }

            await _staffRepository.AddStaffAsync(staff);
            await _emailService.SendCredentialsEmailAsync(staff.Email!, staff.Password, "Staff", staff.FirstName!);
            return (true, null, null, staff.StaffId);
        }

        public async Task UpdateStaffAsync(string staffId, Staff staff)
        {
            // Password is not required, so we can just update the staff details without it.
            await _staffRepository.UpdateStaffAsync(staffId, staff);
        }

        public async Task DeleteStaffAsync(string staffId)
        {
            // Get the staff by ID
            var staff = await _staffRepository.GetStaffByIdAsync(staffId);

            // Check if staff is null
            if (staff == null)
            {
                return; // Exit if staff is not found
            }

            if (!string.IsNullOrEmpty(staff.Email))
            {
                try
                {
                    var userRecord = await FirebaseAuth.DefaultInstance.GetUserByEmailAsync(staff.Email);
                    if (userRecord != null)
                    {
                        await FirebaseAuth.DefaultInstance.DeleteUserAsync(userRecord.Uid);
                    }
                }
                catch (Exception)
                {
                    // Handle exceptions (e.g., user not found, invalid token)
                }
            }
            await _staffRepository.DeleteStaffAsync(staffId);
            // Send account deletion email
            await _emailService.SendAccountDeletedEmailAsync(staff.Email!, staff.FirstName ?? "User"); // Default to "User" if FirstName is null
        }

        public async Task<(bool Success, string? ErrorCode, string? ErrorMessage)> LoginAsync(string email, string password)
        {
            var firebaseSection = _configuration.GetSection("Firebase");
            var apiKey = firebaseSection["ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
                return (false, "NO_API_KEY", "Firebase API key is not configured.");

            var result = await Firebase.FirebaseAuthHelper.LoginWithEmailPasswordAsync(apiKey, email, password);
            if (!result.Success)
                return (false, result.ErrorCode, result.ErrorMessage);

            return (true, null, null);
        }

        public async Task<(bool Success, string? ErrorCode, string? ErrorMessage)> UpdatePasswordAsync(string email, string oldPassword, string newPassword)
        {
            var firebaseSection = _configuration.GetSection("Firebase");
            var apiKey = firebaseSection["ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
                return (false, "NO_API_KEY", "Firebase API key is not configured.");

            var loginResult = await Firebase.FirebaseAuthHelper.LoginWithEmailPasswordAsync(apiKey, email, oldPassword);
            if (!loginResult.Success)
                return (false, loginResult.ErrorCode, "Invalid email or password.");

            var updateResult = await Firebase.FirebaseAuthHelper.UpdatePasswordAsync(apiKey, loginResult.IdToken!, newPassword);
            if (!updateResult.Success)
                return (false, updateResult.ErrorCode, updateResult.ErrorMessage);

            return (true, null, null);
        }

        public async Task<string> GeneratePasswordResetLinkAsync(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                throw new ArgumentException("Email cannot be null or empty.", nameof(email));
            }

            try
            {
                // Generate the password reset link
                string link = await FirebaseAuth.DefaultInstance.GeneratePasswordResetLinkAsync(email);
                string oobCode = ExtractOobCodeFromLink(link);

                // Retrieve the admin's details to get the name
                var staff = await _staffRepository.GetStaffByEmailAsync(email); // Assuming email is used as ID or modify accordingly
                string recipientName = staff?.FirstName ?? "User"; // Default to "User" if name is not available
                
                // Send the password reset email
                await _emailService.SendPasswordResetEmailAsync(email, oobCode, recipientName);
                
                return link;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to generate password reset link.", ex);
            }
        }

        public async Task<bool> ResetPasswordAsync(string email, string newPassword)
        {
            try
            {
                // Get the user by email
                var userRecord = await FirebaseAuth.DefaultInstance.GetUserByEmailAsync(email);
                if (userRecord == null)
                {
                    throw new InvalidOperationException("User not found.");
                }

                // Update the password
                var updateArgs = new UserRecordArgs
                {
                    Uid = userRecord.Uid,
                    Password = newPassword
                };

                await FirebaseAuth.DefaultInstance.UpdateUserAsync(updateArgs);
                return true; // Password updated successfully
            }
            catch (Exception ex)
            {
                // Handle exceptions (e.g., user not found, invalid token)
                throw new InvalidOperationException("Failed to reset password.", ex);
            }
        }

        private string ExtractOobCodeFromLink(string link)
        {
            if (string.IsNullOrEmpty(link))
            {
                throw new ArgumentException("Link cannot be null or empty.", nameof(link));
            }

            // Find the start index of the OOB code
            var startIndex = link.IndexOf("&oobCode=") + "&oobCode=".Length;
            if (startIndex < 0)
            {
                throw new InvalidOperationException("OOB code not found in the link.");
            }

            // Find the end index of the OOB code
            var endIndex = link.IndexOf("&apiKey=", startIndex);
            if (endIndex < 0)
            {
                throw new InvalidOperationException("API key not found in the link.");
            }

            // Extract the OOB code
            var oobCode = link.Substring(startIndex, endIndex - startIndex);
            return oobCode;
        }

        public async Task<List<Staff>> GetStaffByRoleAsync(string role)
        {
            return await _staffRepository.GetStaffByRoleAsync(role);
        }

        public async Task<string> UploadProfilePictureAsync(Stream profileImage, string fileName)
        {
            // Logic to upload the profile image using DriveImageService
            return await _driveImageService.UploadImageAsync(profileImage, fileName);
        }

        public async Task<byte[]> GetProfilePictureAsync(string profilePictureUrl)
        {
            if (string.IsNullOrEmpty(profilePictureUrl))
            {
                throw new ArgumentException("Profile picture URL cannot be null or empty.", nameof(profilePictureUrl));
            }

            // Extract the file ID from the URL
            var fileId = profilePictureUrl.Split('=')[1];
            byte[] imageBytes = await _driveImageService.GetImageAsync(profilePictureUrl);
            
            return imageBytes; // Return the image as a byte array
        }

        public async Task<string> UpdateProfilePictureAsync(string staffId, Stream profileImage, string fileName)
        {
            // Logic to update the profile image using DriveImageService
            var staff = await _staffRepository.GetStaffByIdAsync(staffId);
            if (staff == null)
            {
                throw new InvalidOperationException("Staff not found.");
            }

            // Upload the new image
            var newImageUrl = await _driveImageService.UploadImageAsync(profileImage, fileName);
            staff.ProfilePicture = newImageUrl;
            await _staffRepository.UpdateStaffAsync(staffId, staff); // Ensure to update the staff record
            return newImageUrl;
        }

        public async Task<string?> GetStaffIdByEmailAsync(string email)
        {
            var staff = await _staffRepository.GetStaffByEmailAsync(email);
            return staff?.StaffId; // Return the staff ID or null if not found
        }

        public async Task<string> UploadImageAsync(IFormFile imageFile)
        {
            if (imageFile == null || imageFile.Length == 0)
            {
                throw new ArgumentException("File is empty or null.", nameof(imageFile));
            }

            // Save the image to a temporary location before uploading
            var tempFilePath = Path.Combine(Path.GetTempPath(), $"staff_image_{DateTime.UtcNow.Ticks}.jpg");
            using (var stream = new FileStream(tempFilePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(stream);
            }

            // Use the UploadImageTutorialAsync method to upload the image
            return await _driveImageService.UploadImageTutorialAsync(tempFilePath);
        }
    }
}