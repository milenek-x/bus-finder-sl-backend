using Google.Cloud.Firestore;

namespace BusFinderBackend.Model
{
    [FirestoreData]
    public class BusShift
    {
        [FirestoreDocumentId]
        public string? ShiftId { get; set; } // Primary Key

        [FirestoreProperty]
        public string? StartTime { get; set; }

        [FirestoreProperty]
        public string? EndTime { get; set; }

        [FirestoreProperty]
        public string? Date { get; set; }

        [FirestoreProperty]
        public string? RouteNo { get; set; }
    }
} 