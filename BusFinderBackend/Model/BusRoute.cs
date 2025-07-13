using Google.Cloud.Firestore;

namespace BusFinderBackend.Model
{
    [FirestoreData]
    public class BusRoute
    {
        [FirestoreDocumentId]
        public string? RouteNumber { get; set; }

        [FirestoreProperty]
        public string? RouteName { get; set; }

        [FirestoreProperty]
        public List<string>? RouteStops { get; set; } // List of StopNames

        [FirestoreProperty]
        public double? RouteDistance { get; set; } // Distance in kilometers (nullable)
    }   
}