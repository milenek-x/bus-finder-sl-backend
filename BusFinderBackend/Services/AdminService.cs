using BusFinderBackend.Firebase;
using BusFinderBackend.Model;
using BusFinderBackend.Repositories;
using FirebaseAdmin.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Auth.OAuth2;
using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using Microsoft.Net.Http.Headers;
using BusFinderBackend.Services;

namespace BusFinderBackend.Services
{
    public class AdminService
    {
        private readonly AdminRepository _adminRepository;
        private readonly IConfiguration _configuration;
        private readonly EmailService _emailService;
        private readonly ILogger<AdminService> _logger;
        private readonly DriveImageService _driveImageService;
    
        public AdminService(AdminRepository adminRepository, IConfiguration configuration, EmailService emailService, ILogger<AdminService> logger, DriveImageService driveImageService)
        {
            _adminRepository = adminRepository;
            _configuration = configuration;
            _emailService = emailService;
            _logger = logger;
            _driveImageService = driveImageService;
        }

        public Task<List<Admin>> GetAllAdminsAsync()
        {
            return _adminRepository.GetAllAdminsAsync();
        }

        public Task<Admin?> GetAdminByIdAsync(string adminId)
        {
            return _adminRepository.GetAdminByIdAsync(adminId);
        }

        public async Task<(bool Success, string? ErrorCode, string? ErrorMessage, string? AdminId)> AddAdminAsync(Admin admin)
        {
            var firebaseSection = _configuration.GetSection("Firebase");
            var apiKey = firebaseSection["ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
            {
                return (false, "NO_API_KEY", "Firebase API key is not configured.", null);
            }

            if (string.IsNullOrEmpty(admin.AdminId))
            {
                admin.AdminId = await _adminRepository.GenerateNextAdminIdAsync();
            }

            // Check for null or empty values
            if (string.IsNullOrEmpty(admin.Email) || string.IsNullOrEmpty(admin.Password))
            {
                return (false, "INVALID_INPUT", "Email and password must be provided.", null);
            }

            var result = await FirebaseAuthHelper.CreateUserAsync(apiKey, admin.Email, admin.Password);

            if (!result.Success)
            {
                if (result.ErrorCode == "EMAIL_EXISTS")
                {
                    return (false, "EMAIL_EXISTS", "This email is already registered.", null);
                }
                return (false, result.ErrorCode, result.ErrorMessage, null);
            }

            var adminId = await _adminRepository.AddAdminAsync(admin);
            await _emailService.SendCredentialsEmailAsync(admin.Email, admin.Password, "Admin", admin.FirstName!);
            return (true, null, null, adminId);
        }

        public Task UpdateAdminAsync(string adminId, Admin admin)
        {
            return _adminRepository.UpdateAdminAsync(adminId, admin);
        }

        public async Task DeleteAdminAsync(string adminId)
        {
            // Get the admin by ID
            var admin = await _adminRepository.GetAdminByIdAsync(adminId);
            
            // Check if admin is null
            if (admin == null)
            {
                return; // Exit if admin is not found
            }

            // Check if email is not null or empty
            if (!string.IsNullOrEmpty(admin.Email))
            {
                try
                {
                    var userRecord = await FirebaseAuth.DefaultInstance.GetUserByEmailAsync(admin.Email);
                    if (userRecord != null)
                    {
                        await FirebaseAuth.DefaultInstance.DeleteUserAsync(userRecord.Uid);
                    }
                }
                catch (Exception)
                {
                }
            }

            // Delete the admin from the repository
            await _adminRepository.DeleteAdminAsync(adminId);

            // Send account deletion email
            await _emailService.SendAccountDeletedEmailAsync(admin.Email!, admin.FirstName ?? "User"); // Default to "User" if FirstName is null
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
                var admin = await _adminRepository.GetAdminByEmailAsync(email); // Use the new method
                string recipientName = admin?.FirstName ?? "User"; // Default to "User" if name is not available

                // Store the oobCode in Firestore
                await _adminRepository.StoreOobCodeAsync(email, oobCode);

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

        public async Task<string> UploadProfilePictureAsync(Stream profileImage, string fileName)
        {
            return await _driveImageService.UploadImageAsync(profileImage, fileName);
        }

        public async Task<bool> VerifyOobCodeAsync(string email, string oobCode)
        {
            // Retrieve the stored oobCode for the given email
            string? storedOobCode = await _adminRepository.RetrieveOobCodeAsync(email);

            // Compare the provided oobCode with the stored one
            if (storedOobCode == oobCode)
            {
                // If valid, delete the oobCode from Firestore
                await _adminRepository.DeleteOobCodeAsync(email);
                return true;
            }
            return false;
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

        public async Task<string> UpdateProfilePictureAsync(string adminId, Stream profileImage, string fileName)
        {
            var admin = await _adminRepository.GetAdminByIdAsync(adminId);
            if (admin == null)
            {
                throw new InvalidOperationException("Admin not found.");
            }

            // Upload the new image using DriveImageService
            var newImageUrl = await _driveImageService.UploadImageAsync(profileImage, fileName);
            admin.ProfilePicture = newImageUrl; // Update the admin's profile picture URL
            await _adminRepository.UpdateAdminAsync(adminId, admin); // Ensure to update the admin record
            return newImageUrl;
        }

        public async Task<string?> GetAdminIdByEmailAsync(string email)
        {
            var admin = await _adminRepository.GetAdminByEmailAsync(email);
            return admin?.AdminId; // Return the admin ID or null if not found
        }
    }
}
