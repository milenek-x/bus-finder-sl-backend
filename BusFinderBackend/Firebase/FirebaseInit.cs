using Google.Cloud.Firestore;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Configuration;
using System;

namespace BusFinderBackend.Firebase
{
    public static class FirebaseInit
    {
        public static FirestoreDb InitializeFirestore(IConfiguration configuration)
        {
            // Retrieve the Project ID from config (still fine to use this)
            var firebaseSection = configuration.GetSection("Firebase");
            var projectId = firebaseSection["ProjectId"];

            // Get the credential JSON from environment variable
            var credentialJson = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS_JSON");

            if (string.IsNullOrWhiteSpace(credentialJson))
            {
                throw new InvalidOperationException("Missing Google credentials JSON in environment variable.");
            }

            var builder = new FirestoreDbBuilder
            {
                ProjectId = projectId,
                Credential = GoogleCredential.FromJson(credentialJson)
            };

            return builder.Build();
        }
    }
}
