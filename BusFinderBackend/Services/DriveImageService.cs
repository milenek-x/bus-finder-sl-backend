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

        private GoogleCredential GetGoogleCredential(string clientEmail, string privateKey)
        {
            if (string.IsNullOrEmpty(clientEmail))
            {
                throw new ArgumentException("Client email cannot be null or empty.", nameof(clientEmail));
            }

            if (string.IsNullOrEmpty(privateKey))
            {
                throw new ArgumentException("Private key cannot be null or empty.", nameof(privateKey));
            }

            var credentialJson = $@"{{
                ""type"": ""service_account"",
                ""project_id"": ""{_configuration["GoogleDrive:ProjectId"]}"",
                ""private_key_id"": ""{_configuration["GoogleDrive:PrivateKeyId"]}"",
                ""private_key"": ""{privateKey.Replace("\n", "\n")}"",
                ""client_email"": ""{clientEmail}"",
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
            _logger.LogInformation($"Retrieved Folder ID: {folderId}");

            if (string.IsNullOrEmpty(folderId))
                throw new InvalidOperationException("Missing Google Drive folder ID.");

            var serviceAccounts = _configuration.GetSection("GoogleDrive:ServiceAccounts").Get<List<ServiceAccount>>();
            if (serviceAccounts == null || serviceAccounts.Count == 0)
            {
                throw new InvalidOperationException("No service accounts configured.");
            }
            
            foreach (var account in serviceAccounts)
            {
                if (string.IsNullOrEmpty(account.ClientEmail) || string.IsNullOrEmpty(account.PrivateKey))
                {
                    _logger.LogWarning("Skipping service account due to missing credentials.");
                    continue;
                }
                _logger.LogInformation($"Using service account: {account.ClientEmail}");
                _logger.LogInformation($"Attempting to upload image to folder ID: {folderId}");
                var credential = GetGoogleCredential(account.ClientEmail, account.PrivateKey);
                var service = new DriveService(new BaseClientService.Initializer
                {
                    HttpClientInitializer = credential,
                    ApplicationName = "BusFinderSL"
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
                if (uploadStatus.Status == Google.Apis.Upload.UploadStatus.Completed)
                {
                    var uploadedFile = request.ResponseBody;
                    if (uploadedFile != null && uploadedFile.Id != null)
                    {
                        return $"https://drive.google.com/uc?id={uploadedFile.Id}";
                    }
                }
            }

            throw new Exception("Upload failed: no file metadata returned from any service account.");
        }

        public async Task<string> UploadImageTutorialAsync(string filePath)
        {
            try
            {
                var serviceAccounts = _configuration.GetSection("GoogleDrive:ServiceAccounts").Get<List<ServiceAccount>>();
                if (serviceAccounts == null || serviceAccounts.Count == 0)
                {
                    throw new InvalidOperationException("No service accounts configured.");
                }

                // Use the first service account for demonstration
                var account = serviceAccounts[0];
                if (string.IsNullOrEmpty(account.ClientEmail) || string.IsNullOrEmpty(account.PrivateKey))
                {
                    throw new InvalidOperationException("Service account credentials are missing.");
                }

                var credential = GetGoogleCredential(account.ClientEmail, account.PrivateKey);

                var service = new DriveService(new BaseClientService.Initializer
                {
                    HttpClientInitializer = credential,
                    ApplicationName = "Drive API Snippets"
                });

                var fileMetadata = new Google.Apis.Drive.v3.Data.File()
                {
                    Name = Path.GetFileName(filePath)
                };
                FilesResource.CreateMediaUpload request;
                using (var stream = new FileStream(filePath, FileMode.Open))
                {
                    request = service.Files.Create(fileMetadata, stream, "image/jpeg");
                    request.Fields = "id";
                    await request.UploadAsync();
                }

                var file = request.ResponseBody;
                Console.WriteLine("File ID: " + file.Id);
                if (file == null || string.IsNullOrEmpty(file.Id))
                {
                    throw new InvalidOperationException("Upload failed: no file metadata returned.");
                }

                return $"https://drive.google.com/uc?id={file.Id}";
            }
            catch (Exception e)
            {
                if (e is AggregateException)
                {
                    Console.WriteLine("Credential Not found");
                    return e.ToString();

                }
                else if (e is FileNotFoundException)
                {
                    Console.WriteLine("File not found");
                    return e.ToString();
                }
                else
                {
                    throw;
                }
            }
        }

        public async Task<byte[]> GetImageAsync(string imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl))
            {
                throw new ArgumentException("Image URL cannot be null or empty.", nameof(imageUrl));
            }

            // Extract the file ID from the URL
            var fileId = imageUrl.Split('=')[1];

            var serviceAccounts = _configuration.GetSection("GoogleDrive:ServiceAccounts").Get<List<ServiceAccount>>();
            if (serviceAccounts == null || serviceAccounts.Count == 0)
            {
                throw new InvalidOperationException("No service accounts configured.");
            }

            foreach (var account in serviceAccounts)
            {
                if (string.IsNullOrEmpty(account.ClientEmail) || string.IsNullOrEmpty(account.PrivateKey))
                {
                    _logger.LogWarning("Skipping service account due to missing credentials.");
                    continue;
                }
                var credential = GetGoogleCredential(account.ClientEmail, account.PrivateKey);
                var service = new DriveService(new BaseClientService.Initializer
                {
                    HttpClientInitializer = credential,
                    ApplicationName = "BusFinderSL"
                });

                // Fetch the file from Google Drive
                var request = service.Files.Get(fileId);
                using var stream = new MemoryStream();
                await request.DownloadAsync(stream);
                return stream.ToArray(); // Return the image as a byte array
            }

            throw new Exception("Failed to retrieve image: no service account could access the file.");
        }

        private class ServiceAccount
        {
            public string? ClientEmail { get; set; }
            public string? PrivateKey { get; set; }
        }
    }
} 