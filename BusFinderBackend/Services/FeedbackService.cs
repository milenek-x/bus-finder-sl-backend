using BusFinderBackend.Model;
using BusFinderBackend.Repositories;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BusFinderBackend.Services
{
    public class FeedbackService
    {
        private readonly FeedbackRepository _feedbackRepository;
        private readonly PassengerRepository _passengerRepository;
        private readonly AdminRepository _adminRepository;

        public FeedbackService(FeedbackRepository feedbackRepository, PassengerRepository passengerRepository, AdminRepository adminRepository)
        {
            _feedbackRepository = feedbackRepository;
            _passengerRepository = passengerRepository;
            _adminRepository = adminRepository;
        }

        public async Task<List<Feedback>> GetAllFeedbacksAsync()
        {
            return await _feedbackRepository.GetAllFeedbacksAsync();
        }

        public async Task<Feedback?> GetFeedbackByIdAsync(string feedbackId)
        {
            return await _feedbackRepository.GetFeedbackByIdAsync(feedbackId);
        }

        private string GenerateFeedbackId()
        {
            return Guid.NewGuid().ToString(); // Auto-generate FeedbackId using GUID
        }

        public async Task<(bool Success, string? ErrorCode, string? ErrorMessage)> AddFeedbackAsync(Feedback feedback)
        {
            // Check for null or empty PassengerId
            if (string.IsNullOrEmpty(feedback.PassengerId))
            {
                return (false, "INVALID_PASSENGER", "Passenger ID cannot be null or empty.");
            }

            // Check if PassengerId exists
            var passenger = await _passengerRepository.GetPassengerByIdAsync(feedback.PassengerId);
            if (passenger == null)
            {
                return (false, "INVALID_PASSENGER", "Passenger ID does not exist.");
            }

            // Check for null or empty AdminId
            if (string.IsNullOrEmpty(feedback.AdminId))
            {
                return (false, "INVALID_ADMIN", "Admin ID cannot be null or empty.");
            }

            // Check if AdminId exists
            var admin = await _adminRepository.GetAdminByIdAsync(feedback.AdminId);
            if (admin == null)
            {
                return (false, "INVALID_ADMIN", "Admin ID does not exist.");
            }

            // Set the created time and auto-generate FeedbackId
            feedback.CreatedTime = DateTime.UtcNow;
            feedback.FeedbackId = GenerateFeedbackId(); // Auto-generate FeedbackId

            await _feedbackRepository.AddFeedbackAsync(feedback);
            return (true, null, null);
        }

        public async Task<(bool Success, string? ErrorCode, string? ErrorMessage)> UpdateFeedbackAsync(string feedbackId, Feedback feedback)
        {
            // Check for null or empty AdminId
            if (string.IsNullOrEmpty(feedback.AdminId))
            {
                return (false, "INVALID_ADMIN", "Admin ID cannot be null or empty.");
            }

            // Check if AdminId exists
            var admin = await _adminRepository.GetAdminByIdAsync(feedback.AdminId);
            if (admin == null)
            {
                return (false, "INVALID_ADMIN", "Admin ID does not exist.");
            }

            // Update only the specified fields
            feedback.RepliedTime = DateTime.UtcNow; // Set the replied time
            await _feedbackRepository.UpdateFeedbackAsync(feedbackId, feedback);
            return (true, null, null);
        }

        public async Task DeleteFeedbackAsync(string feedbackId)
        {
            await _feedbackRepository.DeleteFeedbackAsync(feedbackId);
        }
    }
} 