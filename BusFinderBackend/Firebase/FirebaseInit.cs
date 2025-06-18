using Google.Cloud.Firestore;
using Microsoft.Extensions.Configuration;

namespace BusFinderBackend.Firebase
{
    public static class FirebaseInit
    {
        public static FirestoreDb InitializeFirestore(IConfiguration configuration)
        {
            var firebaseSection = configuration.GetSection("Firebase");
            var credentialPath = firebaseSection["ServiceAccountPath"];
            var projectId = firebaseSection["ProjectId"];

            var builder = new FirestoreDbBuilder
            {
                ProjectId = projectId,
                Credential = Google.Apis.Auth.OAuth2.GoogleCredential.FromFile(credentialPath)
            };

            return builder.Build();
        }
    }
}