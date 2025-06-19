using Google.Cloud.Firestore;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;

namespace BusFinderBackend.Firebase
{
    public static class FirebaseInit
    {
        public static FirestoreDb InitializeFirestore(IConfiguration configuration)
        {
            // Retrieve the Project ID from config (still fine to use this)
            var firebaseSection = configuration.GetSection("Firebase");
            var projectId = firebaseSection["ProjectId"];

            // Try to get the credential JSON from environment variable
            var credentialJson = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS_JSON");
            GoogleCredential credential;

            if (!string.IsNullOrWhiteSpace(credentialJson))
            {
                credential = GoogleCredential.FromJson(credentialJson);
            }
            else
            {
                // Fallback: try to get the file path from env or config
                var credentialPath = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");
                if (string.IsNullOrWhiteSpace(credentialPath))
                {
                    credentialPath = firebaseSection["CredentialsFilePath"];
                }
                if (string.IsNullOrWhiteSpace(credentialPath) || !File.Exists(credentialPath))
                {
                    throw new InvalidOperationException("Missing Google credentials: neither JSON env var nor valid file path found.");
                }
                credential = GoogleCredential.FromFile(credentialPath);
            }

            var builder = new FirestoreDbBuilder
            {
                ProjectId = projectId,
                Credential = credential
            };

            return builder.Build();
        }
    }
}
