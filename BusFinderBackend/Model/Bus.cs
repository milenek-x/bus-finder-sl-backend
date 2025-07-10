using Google.Cloud.Firestore;

namespace BusFinderBackend.Model
{
    [FirestoreData]
    public class Bus
    {
        [FirestoreDocumentId]
        public string? NumberPlate { get; set; } // Primary Key

        [FirestoreProperty]
        public string? BusType { get; set; } = null;

        [FirestoreProperty]
        public string? DriverId { get; set; } = null;

        [FirestoreProperty]
        public string? ConductorId { get; set; } = null;

        [FirestoreProperty]
        public bool BusCapacity { get; set; } = false; // Default to false

        [FirestoreProperty]
        public bool SosStatus { get; set; } = false; // Default to false

        [FirestoreProperty]
        public string? BusRouteNumber { get; set; } = null;

        [FirestoreProperty]
        public double? CurrentLocationLatitude { get; set; }

        [FirestoreProperty]
        public double? CurrentLocationLongitude { get; set; }
    }
}
