using BusFinderBackend.Model;
using BusFinderBackend.Repositories;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System;
using Microsoft.Extensions.Configuration;
using FirebaseAdmin.Auth;
using Microsoft.Extensions.Logging;
using System.IO;
using BusFinderBackend.Services;

namespace BusFinderBackend.Services
{
    public class PassengerService
    {
        private readonly PassengerRepository _passengerRepository;
        private readonly PlaceRepository _placeRepository;
        private readonly IConfiguration _configuration;
        private readonly EmailService _emailService;
        private readonly ILogger<PassengerService> _logger;
        private readonly DriveImageService _driveImageService;

        public PassengerService(PassengerRepository passengerRepository, PlaceRepository placeRepository, IConfiguration configuration, EmailService emailService, ILogger<PassengerService> logger, DriveImageService driveImageService)
        {
            _passengerRepository = passengerRepository;
            _placeRepository = placeRepository;
            _configuration = configuration;
            _emailService = emailService;
            _logger = logger;
            _driveImageService = driveImageService;
        }

        public async Task<List<Passenger>> GetAllPassengersAsync()
        {
            return await _passengerRepository.GetAllPassengersAsync();
        }

        public async Task<Passenger?> GetPassengerByIdAsync(string passengerId)
        {
            return await _passengerRepository.GetPassengerByIdAsync(passengerId);
        }

        public async Task<(bool Success, string? ErrorCode, string? ErrorMessage, string? passengerId)> AddPassengerAsync(Passenger passenger)
        {
            if (string.IsNullOrEmpty(passenger.PassengerId))
            {
                passenger.PassengerId = await _passengerRepository.GenerateNextPassengerIdAsync();
            }

            // Check for null or empty values
            if (string.IsNullOrEmpty(passenger.Email) || string.IsNullOrEmpty(passenger.Password))
            {
                return (false, "INVALID_INPUT", "Email and password must be provided.", null);
            }

            var firebaseSection = _configuration.GetSection("Firebase");
            var apiKey = firebaseSection["ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
            {
                return (false, "NO_API_KEY", "Firebase API key is not configured.", null);
            }

            var firebaseResult = await Firebase.FirebaseAuthHelper.CreateUserAsync(apiKey, passenger.Email, passenger.Password);

            if (!firebaseResult.Success)
            {
                return (false, firebaseResult.ErrorCode, firebaseResult.ErrorMessage, null);
            }

            // Add the passenger to the repository
            await _passengerRepository.AddPassengerAsync(passenger);

            return (true, null, null, passenger.PassengerId);
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

                // Store the oobCode in Firestore
                await _passengerRepository.StoreOobCodeAsync(email, oobCode);

                // Retrieve the admin's details to get the name
                var passenger = await _passengerRepository.GetPassengerByEmailAsync(email); // Use the new method
                string recipientName = passenger?.FirstName ?? "User"; // Default to "User" if name is not available
                // Send the password reset email
                await _emailService.SendPasswordResetEmailAsync(email, oobCode, recipientName);
                return link;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to generate password reset link.", ex);
            }
        }

        public async Task<bool> VerifyOobCodeAsync(string email, string oobCode)
        {
            // Retrieve the stored oobCode for the given email
            string? storedOobCode = await _passengerRepository.RetrieveOobCodeAsync(email);

            // Compare the provided oobCode with the stored one
            if (storedOobCode == oobCode)
            {
                // If valid, delete the oobCode from Firestore
                await _passengerRepository.DeleteOobCodeAsync(email);
                return true;
            }
            return false;
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

        public async Task AddFavoritePlaceAsync(string passengerId, Place place)
        {
            var passenger = await _passengerRepository.GetPassengerByIdAsync(passengerId);
            if (passenger == null)
                throw new InvalidOperationException("Passenger not found.");
            if (passenger.FavoritePlaces == null)
                passenger.FavoritePlaces = new List<Place>();
            // Check for duplicate by name and coordinates
            if (!passenger.FavoritePlaces.Any(p => p.PlaceName == place.PlaceName && p.Latitude == place.Latitude && p.Longitude == place.Longitude))
            {
                // Generate place ID using the format: placeName-latitude-longitude
                // Sanitize the place name to remove special characters that might cause issues in document IDs
                var sanitizedPlaceName = System.Text.RegularExpressions.Regex.Replace(place.PlaceName, @"[^a-zA-Z0-9\s-]", "");
                sanitizedPlaceName = sanitizedPlaceName.Replace(" ", "-");
                var placeId = $"{sanitizedPlaceName}-{place.Latitude}-{place.Longitude}";
                place.PlaceId = placeId;
                
                // Check if the place already exists in the places collection
                var existingPlace = await _placeRepository.GetPlaceByIdAsync(placeId);
                if (existingPlace == null)
                {
                    // Add the place to the places collection only if it doesn't exist
                    await _placeRepository.AddPlaceAsync(place);
                }
                else
                {
                    // Use the existing place data
                    place = existingPlace;
                }
                
                // Add to passenger's favorite places
                passenger.FavoritePlaces.Add(place);
                await _passengerRepository.UpdatePassengerAsync(passengerId, passenger);
            }
        }

        public async Task RemoveFavoritePlaceAsync(string passengerId, Place place)
        {
            var passenger = await _passengerRepository.GetPassengerByIdAsync(passengerId);
            if (passenger == null || passenger.FavoritePlaces == null)
                throw new InvalidOperationException("Passenger not found or no favorite places.");
            var toRemove = passenger.FavoritePlaces.FirstOrDefault(p => p.PlaceName == place.PlaceName && p.Latitude == place.Latitude && p.Longitude == place.Longitude);
            if (toRemove == null)
                throw new InvalidOperationException("The place is not in the passenger's favorite list.");
            passenger.FavoritePlaces.Remove(toRemove);
            await _passengerRepository.UpdatePassengerAsync(passengerId, passenger);
        }

        public async Task RemoveFavoritePlaceAsync(string passengerId, string placeName, double latitude, double longitude)
        {
            var passenger = await _passengerRepository.GetPassengerByIdAsync(passengerId);
            if (passenger == null || passenger.FavoritePlaces == null)
                throw new InvalidOperationException("Passenger not found or no favorite places.");
            
            var toRemove = passenger.FavoritePlaces.FirstOrDefault(p => 
                p.PlaceName == placeName && 
                p.Latitude == latitude && 
                p.Longitude == longitude);
                
            if (toRemove == null)
                throw new InvalidOperationException("The place is not in the passenger's favorite list.");
                
            passenger.FavoritePlaces.Remove(toRemove);
            await _passengerRepository.UpdatePassengerAsync(passengerId, passenger);
            // Note: The place is NOT removed from the places collection - only from the passenger's favorites
        }

        public async Task UpdateLocationAsync(string passengerId, double? latitude, double? longitude)
        {
            var passenger = await _passengerRepository.GetPassengerByIdAsync(passengerId);
            if (passenger != null)
            {
                passenger.CurrentLocationLatitude = latitude ?? 0;
                passenger.CurrentLocationLongitude = longitude ?? 0;
                await _passengerRepository.UpdatePassengerAsync(passengerId, passenger);
            }
        }

        public async Task UpdateAvatarAsync(string passengerId, int avatarId)
        {
            var passenger = await _passengerRepository.GetPassengerByIdAsync(passengerId);
            if (passenger == null)
                throw new InvalidOperationException("Passenger not found.");
            passenger.AvatarId = avatarId;
            await _passengerRepository.UpdatePassengerAsync(passengerId, passenger);
        }

        private void StoreOobCode(string email, string oobCode)
        {
            // Implement logic to store the oobCode associated with the email
            // This could be in-memory storage, a database, or any other temporary storage
        }

        private string RetrieveOobCode(string email)
        {
            // Implement logic to retrieve the stored oobCode for the given email
            // This could be in-memory storage, a database, or any other temporary storage
            throw new NotImplementedException();
        }

        public async Task<(List<Place>? FavoritePlaces, List<string>? FavoriteRoutes)> GetFavoritesAsync(string passengerId)
        {
            var passenger = await _passengerRepository.GetPassengerByIdAsync(passengerId);
            if (passenger == null)
                throw new InvalidOperationException("Passenger not found.");
            return (passenger.FavoritePlaces, passenger.FavoriteRoutes);
        }

        public async Task<List<Place>?> GetFavoritePlacesAsync(string passengerId)
        {
            var passenger = await _passengerRepository.GetPassengerByIdAsync(passengerId);
            if (passenger == null)
                throw new InvalidOperationException("Passenger not found.");
            return passenger.FavoritePlaces;
        }

        public async Task<List<string>?> GetFavoriteRoutesAsync(string passengerId)
        {
            var passenger = await _passengerRepository.GetPassengerByIdAsync(passengerId);
            if (passenger == null)
                throw new InvalidOperationException("Passenger not found.");
            return passenger.FavoriteRoutes;
        }
    }
} 