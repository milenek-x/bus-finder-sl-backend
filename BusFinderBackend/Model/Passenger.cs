using Google.Cloud.Firestore;

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
        public string? ProfilePicture { get; set; }

        [FirestoreProperty]
        public string? Email { get; set; }

        [FirestoreProperty]
        public string? Password { get; set; }

        [FirestoreProperty]
        public List<string>? FavoriteRoutes { get; set; } // List of favorite route IDs

        [FirestoreProperty]
        public List<string>? FavoritePlaces { get; set; } // List of favorite place names or IDs

        [FirestoreProperty]
        public double CurrentLocationLatitude { get; set; }

        [FirestoreProperty]
        public double CurrentLocationLongitude { get; set; }
    }
} 