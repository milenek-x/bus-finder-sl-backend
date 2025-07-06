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
                var admin = await _adminRepository.GetAdminByEmailAsync(email); // Use the new method
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
            var serviceAccountEmail = _configuration["GoogleDrive:ClientEmail"];
            var rawPrivateKey = _configuration["GoogleDrive:PrivateKey"];
            var privateKey = rawPrivateKey?.Replace("\\n", "\n");

            var folderId = _configuration["GoogleDrive:FolderId"]; // This should now be "1OUrlNlD5_sD_QAlC3Qf4IZsz5NMOtFh4"

            if (string.IsNullOrEmpty(serviceAccountEmail) || string.IsNullOrEmpty(privateKey) || string.IsNullOrEmpty(folderId))
                throw new InvalidOperationException("Missing Google Drive credentials or folder ID.");

            var credentialJson = $@"{{
                ""type"": ""service_account"",
                ""project_id"": ""{_configuration["GoogleDrive:ProjectId"]}"",
                ""private_key_id"": ""{_configuration["GoogleDrive:PrivateKeyId"]}"",
                ""private_key"": ""{privateKey}"",
                ""client_email"": ""{serviceAccountEmail}"",
                ""client_id"": ""{_configuration["GoogleDrive:ClientId"]}"",
                ""auth_uri"": ""https://accounts.google.com/o/oauth2/auth"",
                ""token_uri"": ""https://oauth2.googleapis.com/token"",
                ""auth_provider_x509_cert_url"": ""https://www.googleapis.com/oauth2/v1/certs"",
                ""client_x509_cert_url"": ""{_configuration["GoogleDrive:ClientCertUrl"]}""
            }}";

            var credential = GoogleCredential.FromJson(credentialJson)
                .CreateScoped(DriveService.Scope.Drive);

            var service = new DriveService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "YourAppName"
            });

            var fileMetaData = new Google.Apis.Drive.v3.Data.File()
            {
                Name = fileName,
                Parents = new List<string> { folderId }
            };

            using var memoryStream = new MemoryStream();
            await imageStream.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            var request = service.Files.Create(fileMetaData, memoryStream, "image/jpeg");
            request.Fields = "id";
            request.SupportsAllDrives = true; // 💡 important if uploading to shared/team drives

            var uploadStatus = await request.UploadAsync();
            if (uploadStatus.Status != Google.Apis.Upload.UploadStatus.Completed)
            {
                throw new Exception($"Google Drive upload failed: {uploadStatus.Exception?.Message}");
            }

            var uploadedFile = request.ResponseBody;
            if (uploadedFile == null || uploadedFile.Id == null)
            {
                throw new Exception("Upload failed: no file metadata returned.");
            }

            return $"https://drive.google.com/uc?id={uploadedFile.Id}";
        }

        public async Task<byte[]> GetProfilePictureAsync(string profilePictureUrl)
        {
            if (string.IsNullOrEmpty(profilePictureUrl))
            {
                throw new ArgumentException("Profile picture URL cannot be null or empty.", nameof(profilePictureUrl));
            }

            // Extract the file ID from the URL
            var fileId = profilePictureUrl.Split('=')[1];

            // Create a Google Drive service instance
            var serviceAccountEmail = _configuration["GoogleDrive:ClientEmail"];
            var rawPrivateKey = _configuration["GoogleDrive:PrivateKey"];
            var privateKey = rawPrivateKey?.Replace("\\n", "\n");

            var credentialJson = $@"{{
                ""type"": ""service_account"",
                ""project_id"": ""{_configuration["GoogleDrive:ProjectId"]}"",
                ""private_key_id"": ""{_configuration["GoogleDrive:PrivateKeyId"]}"",
                ""private_key"": ""{privateKey}"",
                ""client_email"": ""{serviceAccountEmail}"",
                ""client_id"": ""{_configuration["GoogleDrive:ClientId"]}"",
                ""auth_uri"": ""https://accounts.google.com/o/oauth2/auth"",
                ""token_uri"": ""https://oauth2.googleapis.com/token"",
                ""auth_provider_x509_cert_url"": ""https://www.googleapis.com/oauth2/v1/certs"",
                ""client_x509_cert_url"": ""{_configuration["GoogleDrive:ClientCertUrl"]}""
            }}";

            var credential = GoogleCredential.FromJson(credentialJson)
                .CreateScoped(DriveService.Scope.Drive);

            var service = new DriveService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "YourAppName"
            });

            // Fetch the file from Google Drive
            var request = service.Files.Get(fileId);
            var stream = new MemoryStream();
            await request.DownloadAsync(stream);
            return stream.ToArray(); // Return the image as a byte array
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
    }
}
