using Google.Cloud.Firestore;

namespace BusFinderBackend.Model
{
    [FirestoreData]
    public class ShiftDetails
    {
        [FirestoreProperty]
        public string? StartTime { get; set; }
        [FirestoreProperty]
        public string? EndTime { get; set; }
        [FirestoreProperty]
        public string? Date { get; set; }
    }

    [FirestoreData]
    public class BusShift
    {
        [FirestoreDocumentId]
        public string? ShiftId { get; set; } // Primary Key

        [FirestoreProperty]
        public ShiftDetails? Normal { get; set; }

        [FirestoreProperty]
        public ShiftDetails? Reverse { get; set; }

        [FirestoreProperty]
        public string? RouteNo { get; set; }

        [FirestoreProperty]
        public string? NumberPlate { get; set; } // Number Plate
    }
} 