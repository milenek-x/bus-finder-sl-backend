using Google.Cloud.Firestore;

namespace BusFinderBackend.Model
{
    [FirestoreData]
    public class Place
    {
        [FirestoreDocumentId]
        public string? PlaceId { get; set; } // Primary Key

        [FirestoreProperty]
        public string PlaceName { get; set; } = string.Empty; // Default to empty string

        [FirestoreProperty]
        public double Latitude { get; set; }

        [FirestoreProperty]
        public double Longitude { get; set; }

        [FirestoreProperty]
        public string LocationImage { get; set; } = string.Empty; // Default to empty string
    }
}
