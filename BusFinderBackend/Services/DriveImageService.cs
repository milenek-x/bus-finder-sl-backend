using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace BusFinderBackend.Services
{
    public class DriveImageService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<DriveImageService> _logger;

        public DriveImageService(IConfiguration configuration, ILogger<DriveImageService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        private GoogleCredential GetGoogleCredential()
        {
            var serviceAccountEmail = _configuration["GoogleDrive:ClientEmail"];
            var rawPrivateKey = _configuration["GoogleDrive:PrivateKey"];
            var privateKey = rawPrivateKey?.Replace("\n", "\n");

            if (string.IsNullOrEmpty(serviceAccountEmail) || string.IsNullOrEmpty(privateKey))
                throw new InvalidOperationException("Missing Google Drive credentials.");

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

            return GoogleCredential.FromJson(credentialJson).CreateScoped(DriveService.Scope.Drive);
        }

        public async Task<string> UploadImageAsync(Stream imageStream, string fileName)
        {
            var folderId = _configuration["GoogleDrive:FolderId"]; // Specify your folder ID here

            if (string.IsNullOrEmpty(folderId))
                throw new InvalidOperationException("Missing Google Drive folder ID.");

            var credential = GetGoogleCredential();
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
            request.SupportsAllDrives = true; // Important if uploading to shared/team drives

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

        public async Task<byte[]> GetImageAsync(string imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl))
            {
                throw new ArgumentException("Image URL cannot be null or empty.", nameof(imageUrl));
            }

            // Extract the file ID from the URL
            var fileId = imageUrl.Split('=')[1];

            var credential = GetGoogleCredential();
            var service = new DriveService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "YourAppName"
            });

            // Fetch the file from Google Drive
            var request = service.Files.Get(fileId);
            using var stream = new MemoryStream();
            await request.DownloadAsync(stream);
            return stream.ToArray(); // Return the image as a byte array
        }
    }
} 