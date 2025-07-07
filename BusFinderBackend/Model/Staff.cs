using Google.Cloud.Firestore;

namespace BusFinderBackend.Model
{
    [FirestoreData]
    public class Staff
    {
        [FirestoreDocumentId]
        public string? StaffId { get; set; } // Primary Key

        [FirestoreProperty]
        public string? FirstName { get; set; }

        [FirestoreProperty]
        public string? LastName { get; set; }

        [FirestoreProperty]
        public string? ProfilePicture { get; set; }

        [FirestoreProperty]
        public string? NIC { get; set; }

        [FirestoreProperty]
        public string? StaffRole { get; set; }

        [FirestoreProperty]
        public string? TelNo { get; set; }

        [FirestoreProperty]
        public string? Email { get; set; }
    
        public string? Password { get; set; }
    }
}
