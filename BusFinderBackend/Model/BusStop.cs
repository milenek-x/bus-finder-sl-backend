using Google.Cloud.Firestore;

namespace BusFinderBackend.Model
{
    [FirestoreData]
    public class BusStop
    {
        [FirestoreDocumentId]
        public string? StopName { get; set; }

        [FirestoreProperty]
        public double StopLatitude { get; set; }

        [FirestoreProperty]
        public double StopLongitude { get; set; }
    }
}