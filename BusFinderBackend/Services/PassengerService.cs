using BusFinderBackend.Model;
using BusFinderBackend.Repositories;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System;
using Microsoft.Extensions.Configuration;
using FirebaseAdmin.Auth;

namespace BusFinderBackend.Services
{
    public class PassengerService
    {
        private readonly PassengerRepository _passengerRepository;
        private readonly IConfiguration _configuration;
        private readonly EmailService _emailService;

        public PassengerService(PassengerRepository passengerRepository, IConfiguration configuration, EmailService emailService)
        {
            _passengerRepository = passengerRepository;
            _configuration = configuration;
            _emailService = emailService;
        }

        public async Task<List<Passenger>> GetAllPassengersAsync()
        {
            return await _passengerRepository.GetAllPassengersAsync();
        }

        public async Task<Passenger?> GetPassengerByIdAsync(string passengerId)
        {
            return await _passengerRepository.GetPassengerByIdAsync(passengerId);
        }

        public async Task<(bool Success, string? ErrorCode, string? ErrorMessage)> AddPassengerAsync(Passenger passenger)
        {
            if (string.IsNullOrEmpty(passenger.PassengerId))
            {
                passenger.PassengerId = await _passengerRepository.GenerateNextPassengerIdAsync();
            }

            // Check for null or empty values
            if (string.IsNullOrEmpty(passenger.Email) || string.IsNullOrEmpty(passenger.Password))
            {
                return (false, "INVALID_INPUT", "Email and password must be provided.");
            }

            var firebaseSection = _configuration.GetSection("Firebase");
            var apiKey = firebaseSection["ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
            {
                return (false, "NO_API_KEY", "Firebase API key is not configured.");
            }

            // Create the user in Firebase Authentication
            var firebaseResult = await Firebase.FirebaseAuthHelper.CreateUserAsync(apiKey, passenger.Email, passenger.Password);

            if (!firebaseResult.Success)
            {
                return (false, firebaseResult.ErrorCode, firebaseResult.ErrorMessage);
            }

            // If user creation is successful, add the passenger to the repository
            await _passengerRepository.AddPassengerAsync(passenger);
            return (true, null, null);
        }

        public async Task UpdatePassengerAsync(string passengerId, Passenger passenger)
        {
            await _passengerRepository.UpdatePassengerAsync(passengerId, passenger);
        }

        public async Task DeletePassengerAsync(string passengerId)
        {
            // Get the passenger by ID
            var passenger = await _passengerRepository.GetPassengerByIdAsync(passengerId);
            if (passenger != null && !string.IsNullOrEmpty(passenger.Email))
            {
                try
                {
                    var userRecord = await FirebaseAuth.DefaultInstance.GetUserByEmailAsync(passenger.Email);
                    if (userRecord != null)
                    {
                        await FirebaseAuth.DefaultInstance.DeleteUserAsync(userRecord.Uid);
                    }
                }
                catch (Exception ex)
                {
                    // Log the error if needed
                    throw new InvalidOperationException($"Failed to delete passenger from Firebase Authentication: {passenger.Email}", ex);
                }
            }
            await _passengerRepository.DeletePassengerAsync(passengerId);
        }

        public async Task<(bool Success, string? ErrorCode, string? ErrorMessage)> LoginAsync(string email, string password)
        {
            var firebaseSection = _configuration.GetSection("Firebase");
            var apiKey = firebaseSection["ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
            {
                return (false, "NO_API_KEY", "Firebase API key is not configured.");
            }

            var result = await Firebase.FirebaseAuthHelper.LoginWithEmailPasswordAsync(apiKey, email, password);
            if (!result.Success)
            {
                return (false, result.ErrorCode, result.ErrorMessage);
            }

            return (true, null, null);
        }

        public async Task<(bool Success, string? ErrorCode, string? ErrorMessage)> UpdatePasswordAsync(string email, string oldPassword, string newPassword)
        {
            var firebaseSection = _configuration.GetSection("Firebase");
            var apiKey = firebaseSection["ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
            {
                return (false, "NO_API_KEY", "Firebase API key is not configured.");
            }

            var loginResult = await Firebase.FirebaseAuthHelper.LoginWithEmailPasswordAsync(apiKey, email, oldPassword);
            if (!loginResult.Success)
            {
                return (false, loginResult.ErrorCode, "Invalid email or password.");
            }

            var updateResult = await Firebase.FirebaseAuthHelper.UpdatePasswordAsync(apiKey, loginResult.IdToken!, newPassword);
            if (!updateResult.Success)
            {
                return (false, updateResult.ErrorCode, updateResult.ErrorMessage);
            }

            return (true, null, null);
        }

        public async Task<string> GeneratePasswordResetLinkAsync(string email)
        {
            var firebaseSection = _configuration.GetSection("Firebase");
            var apiKey = firebaseSection["ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
            {
                throw new InvalidOperationException("Firebase API key is not configured.");
            }

            try
            {
                // Generate the password reset link using Firebase
                string link = await FirebaseAuth.DefaultInstance.GeneratePasswordResetLinkAsync(email);
                string oobCode = ExtractOobCodeFromLink(link);

                // Send the password reset email
                await _emailService.SendPasswordResetEmailAsync(email, oobCode);
                return link;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to generate password reset link.", ex);
            }
        }

        public async Task<bool> ResetPasswordAsync(string email, string newPassword)
        {
            var firebaseSection = _configuration.GetSection("Firebase");
            var apiKey = firebaseSection["ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
            {
                throw new InvalidOperationException("Firebase API key is not configured.");
            }

            try
            {
                // Get the user by email
                var userRecord = await FirebaseAuth.DefaultInstance.GetUserByEmailAsync(email);
                if (userRecord == null)
                {
                    throw new InvalidOperationException("User not found.");
                }

                // Update the password
                var updateArgs = new UserRecordArgs
                {
                    Uid = userRecord.Uid,
                    Password = newPassword
                };

                await FirebaseAuth.DefaultInstance.UpdateUserAsync(updateArgs);
                return true; // Password updated successfully
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to reset password.", ex);
            }
        }

        private string ExtractOobCodeFromLink(string link)
        {
            if (string.IsNullOrEmpty(link))
            {
                throw new ArgumentException("Link cannot be null or empty.", nameof(link));
            }

            // Find the start index of the OOB code
            var startIndex = link.IndexOf("&oobCode=") + "&oobCode=".Length;
            if (startIndex < 0)
            {
                throw new InvalidOperationException("OOB code not found in the link.");
            }

            // Find the end index of the OOB code
            var endIndex = link.IndexOf("&apiKey=", startIndex);
            if (endIndex < 0)
            {
                throw new InvalidOperationException("API key not found in the link.");
            }

            // Extract the OOB code
            var oobCode = link.Substring(startIndex, endIndex - startIndex);
            return oobCode;
        }

        public async Task<(bool Success, string? ErrorCode, string? ErrorMessage)> GoogleSignInAsync(string idToken)
        {
            var firebaseSection = _configuration.GetSection("Firebase");
            var apiKey = firebaseSection["ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
            {
                return (false, "NO_API_KEY", "Firebase API key is not configured.");
            }

            var result = await Firebase.FirebaseAuthHelper.GoogleSignInAsync(apiKey, idToken);
            if (!result.Success)
            {
                return (false, result.ErrorCode, result.ErrorMessage);
            }

            return (true, null, null);
        }

        public async Task AddFavoriteRouteAsync(string passengerId, string routeNumber)
        {
            // Assuming routeNumber is the same as routeId for your application logic
            await _passengerRepository.AddFavoriteRouteAsync(passengerId, routeNumber);
        }

        public async Task RemoveFavoriteRouteAsync(string passengerId, string routeId)
        {
            var passenger = await _passengerRepository.GetPassengerByIdAsync(passengerId);
            if (passenger == null || passenger.FavoriteRoutes == null || !passenger.FavoriteRoutes.Contains(routeId))
            {
                throw new InvalidOperationException("The route is not in the passenger's favorite list.");
            }

            await _passengerRepository.RemoveFavoriteRouteAsync(passengerId, routeId);
        }

        public async Task AddFavoritePlaceAsync(string passengerId, string placeId)
        {
            await _passengerRepository.AddFavoritePlaceAsync(passengerId, placeId);
        }

        public async Task RemoveFavoritePlaceAsync(string passengerId, string placeId)
        {
            var passenger = await _passengerRepository.GetPassengerByIdAsync(passengerId);
            if (passenger == null || passenger.FavoritePlaces == null || !passenger.FavoritePlaces.Contains(placeId))
            {
                throw new InvalidOperationException("The place is not in the passenger's favorite list.");
            }

            await _passengerRepository.RemoveFavoritePlaceAsync(passengerId, placeId);
        }

        public async Task UpdateLocationAsync(string passengerId, double latitude, double longitude)
        {
            var passenger = await _passengerRepository.GetPassengerByIdAsync(passengerId);
            if (passenger != null)
            {
                passenger.CurrentLocationLatitude = latitude;
                passenger.CurrentLocationLongitude = longitude;
                await _passengerRepository.UpdatePassengerAsync(passengerId, passenger);
            }
        }
    }
} 