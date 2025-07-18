using Google.Cloud.Firestore;
using System.Text.Json.Serialization;

namespace BusFinderBackend.Model
{
    [FirestoreData]
    public class Passenger
    {
        [FirestoreDocumentId]
        public string? PassengerId { get; set; } // Primary Key

        [FirestoreProperty]
        public string? FirstName { get; set; }

        [FirestoreProperty]
        public string? LastName { get; set; }

        [FirestoreProperty]
        public string? ProfileImageUrl { get; set; }

        [FirestoreProperty]
        public string? Email { get; set; }

        public string? Password { get; set; }

        [FirestoreProperty]
        public List<string>? FavoriteRoutes { get; set; } // List of favorite route IDs

        [FirestoreProperty]
        public List<Place>? FavoritePlaces { get; set; } // List of favorite place objects

        [FirestoreProperty]
        [JsonPropertyName("CurrentLocationLatitude")]
        public double? CurrentLocationLatitude { get; set; }

        [FirestoreProperty]
        [JsonPropertyName("CurrentLocationLongitude")]
        public double? CurrentLocationLongitude { get; set; }
    }
} 