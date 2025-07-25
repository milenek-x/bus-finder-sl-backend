using BusFinderBackend.Repositories;
using System.Collections.Generic;
using System.Threading.Tasks;
using BusFinderBackend.Model;
using BusFinderBackend.DTOs.Feedback;
using BusFinderBackend.Services;

namespace BusFinderBackend.Services
{
    public class FeedbackService
    {
        private readonly FeedbackRepository _feedbackRepository;
        private readonly PassengerRepository _passengerRepository;
        private readonly AdminRepository _adminRepository;
        private readonly NotificationService _notificationService;

        public FeedbackService(FeedbackRepository feedbackRepository, PassengerRepository passengerRepository, AdminRepository adminRepository, NotificationService notificationService)
        {
            _feedbackRepository = feedbackRepository;
            _passengerRepository = passengerRepository;
            _adminRepository = adminRepository;
            _notificationService = notificationService;
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

        public async Task<(bool Success, string? ErrorCode, string? ErrorMessage, Feedback? Feedback, string? FeedbackId)> AddFeedbackAsync(FeedbackCreateDto feedbackDto)
        {
            // Check for null or empty PassengerId
            if (string.IsNullOrEmpty(feedbackDto.PassengerId))
            {
                return (false, "INVALID_PASSENGER", "Passenger ID cannot be null or empty.", null, null);
            }

            // Check if PassengerId exists
            var passenger = await _passengerRepository.GetPassengerByIdAsync(feedbackDto.PassengerId);
            if (passenger == null)
            {
                return (false, "INVALID_PASSENGER", "Passenger ID does not exist.", null, null);
            }

            // Set the created time and auto-generate FeedbackId
            var feedback = new Feedback
            {
                PassengerId = feedbackDto.PassengerId,
                Message = feedbackDto.Message,
                Subject = feedbackDto.Subject,
                CreatedTime = DateTime.UtcNow,
                FeedbackId = GenerateFeedbackId()
            };

            await _feedbackRepository.AddFeedbackAsync(feedback);
            // Notify all (admins, staff, etc.)
            await _notificationService.NotifyAllAsync($"New feedback received: {feedback.Subject}", "FeedbackReceived");
            return (true, null, null, feedback, feedback.FeedbackId);
        }

        public async Task<(bool Success, string? ErrorCode, string? ErrorMessage)> ReplyFeedbackAsync(string feedbackId, FeedbackReplyDto replyDto)
        {
            // Check for null or empty AdminId
            if (string.IsNullOrEmpty(replyDto.AdminId))
            {
                return (false, "INVALID_ADMIN", "Admin ID cannot be null or empty.");
            }

            // Check if AdminId exists
            var admin = await _adminRepository.GetAdminByIdAsync(replyDto.AdminId);
            if (admin == null)
            {
                return (false, "INVALID_ADMIN", "Admin ID does not exist.");
            }

            // Get the existing feedback
            var feedback = await _feedbackRepository.GetFeedbackByIdAsync(feedbackId);
            if (feedback == null)
            {
                return (false, "INVALID_FEEDBACK", "Feedback ID does not exist.");
            }

            // Update only the reply fields
            feedback.AdminId = replyDto.AdminId;
            feedback.Reply = replyDto.Reply;
            feedback.RepliedTime = DateTime.UtcNow;
            await _feedbackRepository.UpdateFeedbackAsync(feedbackId, feedback);
            // Notify the passenger who submitted the feedback
            if (!string.IsNullOrEmpty(feedback.PassengerId))
            {
                await _notificationService.NotifyAllAsync($"Feedback from Passenger {feedback.PassengerId} has been replied: {feedback.Subject}", "FeedbackReplied");
            }
            return (true, null, null);
        }

        public async Task DeleteFeedbackAsync(string feedbackId)
        {
            await _feedbackRepository.DeleteFeedbackAsync(feedbackId);
        }
    }
} 