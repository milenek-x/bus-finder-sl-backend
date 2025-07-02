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

namespace BusFinderBackend.Services
{
    public class AdminService
    {
        private readonly AdminRepository _adminRepository;
        private readonly IConfiguration _configuration;
        private readonly EmailService _emailService;
        private readonly ILogger<AdminService> _logger;
    
        public AdminService(AdminRepository adminRepository, IConfiguration configuration, EmailService emailService, ILogger<AdminService> logger)
        {
            _adminRepository = adminRepository;
            _configuration = configuration;
            _emailService = emailService;
            _logger = logger;
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

            var result = await FirebaseAuthHelper.CreateUserAsync(apiKey, admin.Email!, admin.Password!);

            if (!result.Success)
            {
                if (result.ErrorCode == "EMAIL_EXISTS")
                {
                    return (false, "EMAIL_EXISTS", "This email is already registered.", null);
                }
                return (false, result.ErrorCode, result.ErrorMessage, null);
            }

            var adminId = await _adminRepository.AddAdminAsync(admin);
            await _emailService.SendCredentialsEmailAsync(admin.Email!, admin.Password!, "Admin", admin.FirstName!);
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
                _logger.LogWarning($"Admin with ID {adminId} not found. No deletion performed.");
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
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to delete admin from Firebase Authentication: {admin.Email}");
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
                _logger.LogInformation("Generated password reset link for email: {Email}", email);

                // Retrieve the admin's details to get the name
                var admin = await _adminRepository.GetAdminByIdAsync(email); // Assuming email is used as ID or modify accordingly
                string recipientName = admin?.FirstName ?? "User"; // Default to "User" if name is not available

                // Send the password reset email
                await _emailService.SendPasswordResetEmailAsync(email, oobCode, recipientName);

                return link;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate password reset link for email: {Email}", email);
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

        public async Task<string> UploadProfilePictureAsync(Stream imageStream, string fileName)
        {
            var credential = GoogleCredential.FromFile("path/to/your/credentials.json")
                .CreateScoped(DriveService.Scope.DriveFile);

            var service = new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "Your Application Name",
            });

            var fileMetadata = new Google.Apis.Drive.v3.Data.File()
            {
                Name = fileName,
                MimeType = "image/jpeg" // Adjust based on the image type
            };

            FilesResource.CreateMediaUpload request;
            using (var stream = new MemoryStream())
            {
                await imageStream.CopyToAsync(stream);
                stream.Position = 0; // Reset stream position
                request = service.Files.Create(fileMetadata, stream, "image/jpeg");
                request.Fields = "id";
                await request.UploadAsync();
            }

            var file = request.ResponseBody;
            return $"https://drive.google.com/uc?id={file.Id}"; // Return the link
        }

    }
}
