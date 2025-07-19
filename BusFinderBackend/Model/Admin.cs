using Google.Cloud.Firestore;

namespace BusFinderBackend.Model
{
    [FirestoreData]
    public class Admin
    {
        [FirestoreDocumentId]
        public string? AdminId { get; set; }

        [FirestoreProperty]
        public string? FirstName { get; set; }

        [FirestoreProperty]
        public string? LastName { get; set; }

        [FirestoreProperty]
        public string? TelNo { get; set; }

        [FirestoreProperty]
        public int AvatarId { get; set; }

        [FirestoreProperty]
        public string? Email { get; set; }

        public string? Password { get; set; }
    }
}