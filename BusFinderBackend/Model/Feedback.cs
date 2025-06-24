using System;
using Google.Cloud.Firestore;

namespace BusFinderBackend.Model
{
    [FirestoreData]
    public class Feedback
    {
        [FirestoreDocumentId]
        public string? FeedbackId { get; set; } // Primary Key

        [FirestoreProperty]
        public string? PassengerId { get; set; } // Foreign Key

        [FirestoreProperty]
        public string? AdminId { get; set; } // Foreign Key

        [FirestoreProperty]
        public string? Message { get; set; }

        [FirestoreProperty]
        public string? Reply { get; set; }

        [FirestoreProperty]
        public string? Subject { get; set; }

        [FirestoreProperty]
        public DateTime CreatedTime { get; set; }

        [FirestoreProperty]
        public DateTime? RepliedTime { get; set; } // Nullable if not yet replied
    }
} 