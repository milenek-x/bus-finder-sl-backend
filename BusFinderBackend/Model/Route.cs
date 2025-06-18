using Google.Cloud.Firestore;

namespace BusFinderBackend.Model
{
    [FirestoreData]
    public class Route
    {
        [FirestoreDocumentId]
        public string? RouteNumber { get; set; }

        [FirestoreProperty]
        public string? RouteName { get; set; }

        [FirestoreProperty]
        public string? RouteDescription { get; set; }

        [FirestoreProperty]
        public List<string>? Stops { get; set; } // List of StopNames
    }   
}