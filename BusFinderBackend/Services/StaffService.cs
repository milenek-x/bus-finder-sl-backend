using BusFinderBackend.Repositories;
using BusFinderBackend.Model;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System; // For Exception
using FirebaseAdmin.Auth; // For FirebaseAuth

namespace BusFinderBackend.Services
{
    public class StaffService
    {
        private readonly StaffRepository _staffRepository;
        private readonly IConfiguration _configuration;
        private readonly EmailService _emailService;
        private readonly ILogger<StaffService> _logger;

        public StaffService(
            StaffRepository staffRepository,
            IConfiguration configuration,
            EmailService emailService,
            ILogger<StaffService> logger)
        {
            _staffRepository = staffRepository;
            _configuration = configuration;
            _emailService = emailService;
            _logger = logger;
        }

        public Task<List<Staff>> GetAllStaffAsync()
        {
            return _staffRepository.GetAllStaffAsync();
        }

        public Task<Staff?> GetStaffByIdAsync(string staffId)
        {
            return _staffRepository.GetStaffByIdAsync(staffId);
        }

        public async Task<(bool Success, string? ErrorCode, string? ErrorMessage)> AddStaffAsync(Staff staff)
        {
            if (string.IsNullOrEmpty(staff.StaffId))
            {
                staff.StaffId = await _staffRepository.GenerateNextStaffIdAsync();
            }

            if (string.IsNullOrEmpty(staff.Email) || string.IsNullOrEmpty(staff.Password))
            {
                return (false, "INVALID_INPUT", "Email and password must be provided.");
            }
            
            var firebaseSection = _configuration.GetSection("Firebase");
            var apiKey = firebaseSection["ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
            {
                return (false, "NO_API_KEY", "Firebase API key is not configured.");
            }

            var firebaseResult = await Firebase.FirebaseAuthHelper.CreateUserAsync(apiKey, staff.Email, staff.Password);

            if (!firebaseResult.Success)
            {
                if (firebaseResult.ErrorCode == "EMAIL_EXISTS")
                {
                    return (false, "EMAIL_EXISTS", "This email is already registered.");
                }
                return (false, firebaseResult.ErrorCode, firebaseResult.ErrorMessage);
            }

            await _staffRepository.AddStaffAsync(staff);
            await _emailService.SendCredentialsEmailAsync(staff.Email!, staff.Password!, "Staff", staff.FirstName!);
            return (true, null, null);
        }

        public Task UpdateStaffAsync(string staffId, Staff staff)
        {
            // Password is not required, so we can just update the staff details without it.
            return _staffRepository.UpdateStaffAsync(staffId, staff);
        }

        public async Task DeleteStaffAsync(string staffId)
        {
            // Get the staff by ID
            var staff = await _staffRepository.GetStaffByIdAsync(staffId);
            if (staff != null && !string.IsNullOrEmpty(staff.Email))
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
    }
}