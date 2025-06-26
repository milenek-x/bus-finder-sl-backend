using BusFinderBackend.Model;
using Google.Cloud.Firestore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BusFinderBackend.Repositories
{
    public class FeedbackRepository
    {
        private readonly CollectionReference _feedbackCollection;

        public FeedbackRepository(FirestoreDb firestoreDb)
        {
            _feedbackCollection = firestoreDb.Collection("testFeedbacks"); // Collection name for feedbacks
        }

        public async Task<List<Feedback>> GetAllFeedbacksAsync()
        {
            var feedbacks = new List<Feedback>();
            var snapshot = await _feedbackCollection.GetSnapshotAsync();

            foreach (var document in snapshot.Documents)
            {
                var feedback = document.ConvertTo<Feedback>();
                feedbacks.Add(feedback);
            }

            return feedbacks;
        }

        public async Task<Feedback?> GetFeedbackByIdAsync(string feedbackId)
        {
            var document = await _feedbackCollection.Document(feedbackId).GetSnapshotAsync();
            return document.Exists ? document.ConvertTo<Feedback>() : null;
        }

        public async Task AddFeedbackAsync(Feedback feedback)
        {
            await _feedbackCollection.Document(feedback.FeedbackId).SetAsync(feedback);
        }

        public async Task UpdateFeedbackAsync(string feedbackId, Feedback feedback)
        {
            await _feedbackCollection.Document(feedbackId).SetAsync(feedback, SetOptions.Overwrite);
        }

        public async Task DeleteFeedbackAsync(string feedbackId)
        {
            await _feedbackCollection.Document(feedbackId).DeleteAsync();
        }
    }
} 