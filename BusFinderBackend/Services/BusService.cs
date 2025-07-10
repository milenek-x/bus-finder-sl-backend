using BusFinderBackend.Model;
using BusFinderBackend.Repositories;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BusFinderBackend.Services
{
    public class BusService
    {
        private readonly BusRepository _busRepository;
        private readonly StaffService _staffService;
        private readonly BusRouteRepository _busRouteRepository;
        private readonly IConfiguration _configuration;

        public BusService(BusRepository busRepository, StaffService staffService, BusRouteRepository busRouteRepository, IConfiguration configuration)
        {
            _busRepository = busRepository;
            _staffService = staffService;
            _busRouteRepository = busRouteRepository;
            _configuration = configuration;
        }

        public async Task<List<Bus>> GetAllBusesAsync()
        {
            return await _busRepository.GetAllBusesAsync();
        }

        public async Task<Bus?> GetBusByNumberPlateAsync(string numberPlate)
        {
            return await _busRepository.GetBusByNumberPlateAsync(numberPlate);
        }

        public async Task AddBusAsync(Bus bus)
        {
            // Check if the driver ID is valid
            if (string.IsNullOrEmpty(bus.DriverId))
            {
                throw new ArgumentException("DriverId cannot be null or empty.");
            }

            var driver = await _staffService.GetStaffByIdAsync(bus.DriverId);
            if (driver == null)
            {
                throw new ArgumentException("Invalid DriverId: The specified driver does not exist.");
            }

            // Check if the conductor ID is valid
            if (string.IsNullOrEmpty(bus.ConductorId))
            {
                throw new ArgumentException("ConductorId cannot be null or empty.");
            }

            var conductor = await _staffService.GetStaffByIdAsync(bus.ConductorId);
            if (conductor == null)
            {
                throw new ArgumentException("Invalid ConductorId: The specified conductor does not exist.");
            }

            // Check if the bus route number is valid
            if (string.IsNullOrEmpty(bus.BusRouteNumber))
            {
                throw new ArgumentException("BusRouteNumber cannot be null or empty.");
            }

            var busRoute = await _busRepository.GetBusRouteByNumberAsync(bus.BusRouteNumber);
            if (busRoute == null)
            {
                throw new ArgumentException("Invalid BusRouteNumber: The specified route does not exist.");
            }

            // Set default values for unspecified properties
            bus.BusCapacity = false; // Default value
            bus.SosStatus = false; // Default value
            bus.CurrentLocationLatitude = 0.0; // Default value
            bus.CurrentLocationLongitude = 0.0; // Default value

            await _busRepository.AddBusAsync(bus);
        }

        public async Task UpdateBusAsync(string numberPlate, Bus bus)
        {
            // Check if the bus exists
            var existingBus = await _busRepository.GetBusByNumberPlateAsync(numberPlate);
            if (existingBus == null)
            {
                throw new ArgumentException("Bus not found.");
            }

            // Check if the driver ID is valid
            if (string.IsNullOrEmpty(bus.DriverId))
            {
                throw new ArgumentException("DriverId cannot be null or empty.");
            }

            var driver = await _staffService.GetStaffByIdAsync(bus.DriverId);
            if (driver == null)
            {
                throw new ArgumentException("Invalid DriverId: The specified driver does not exist.");
            }

            // Check if the conductor ID is valid
            if (string.IsNullOrEmpty(bus.ConductorId))
            {
                throw new ArgumentException("ConductorId cannot be null or empty.");
            }

            var conductor = await _staffService.GetStaffByIdAsync(bus.ConductorId);
            if (conductor == null)
            {
                throw new ArgumentException("Invalid ConductorId: The specified conductor does not exist.");
            }

            // Check if the bus route number is valid
            if (string.IsNullOrEmpty(bus.BusRouteNumber))
            {
                throw new ArgumentException("BusRouteNumber cannot be null or empty.");
            }

            var busRoute = await _busRepository.GetBusRouteByNumberAsync(bus.BusRouteNumber);
            if (busRoute == null)
            {
                throw new ArgumentException("Invalid BusRouteNumber: The specified route does not exist.");
            }

            // Update the bus properties
            existingBus.BusType = bus.BusType;
            existingBus.DriverId = bus.DriverId;
            existingBus.ConductorId = bus.ConductorId;
            existingBus.BusRouteNumber = bus.BusRouteNumber;

            await _busRepository.UpdateBusAsync(numberPlate, existingBus);
        }

        public async Task DeleteBusAsync(string numberPlate)
        {
            // Additional business logic can be added here if needed
            await _busRepository.DeleteBusAsync(numberPlate);
        }

        public async Task UpdateBusCapacityAsync(string numberPlate, bool busCapacity)
        {
            await _busRepository.UpdateBusCapacityAsync(numberPlate, busCapacity);
        }

        public async Task UpdateSosStatusAsync(string numberPlate, bool sosStatus)
        {
            await _busRepository.UpdateSosStatusAsync(numberPlate, sosStatus);
        }

        public async Task UpdateCurrentLocationAsync(string numberPlate, double? latitude, double? longitude)
        {
            await _busRepository.UpdateCurrentLocationAsync(numberPlate, latitude, longitude);
        }

        public async Task UpdateBusLocationIfNeededAsync(string numberPlate)
        {
            var bus = await _busRepository.GetBusByNumberPlateAsync(numberPlate);
            if (bus == null)
            {
                throw new ArgumentException("Bus not found.");
            }

            // Check the current location values before making the API call
            var currentLocation = bus.CurrentLocationLatitude.HasValue && bus.CurrentLocationLongitude.HasValue
                ? new { Latitude = bus.CurrentLocationLatitude.Value, Longitude = bus.CurrentLocationLongitude.Value }
                : new { Latitude = 0.0, Longitude = 0.0 };
            Console.WriteLine("Current Location Latitude: " + currentLocation.Latitude);
            Console.WriteLine("Current Location Longitude: " + currentLocation.Longitude);

            var location = await GetBusLocationFromGoogleMapsAsync(numberPlate);

            Console.WriteLine("Location: " + location);
            if (currentLocation.Latitude == 0 && currentLocation.Longitude == 0)
            {
                // Call Google Maps Geolocation API to get the current location
                if (location.HasValue)
                {
                    // Update the bus's location in Firestore
                    await UpdateCurrentLocationAsync(numberPlate, location.Value.latitude, location.Value.longitude);

                    // Log the current location retrieved from Google Maps API
                    Console.WriteLine("Current Location Latitude from GeoLocation: " + location.Value.latitude);
                    Console.WriteLine("Current Location Longitude from GeoLocation: " + location.Value.longitude);
                }
            }
        }

        // Method to call Google Maps Geolocation API
        private async Task<(double latitude, double longitude)?> GetBusLocationFromGoogleMapsAsync(string numberPlate)
        {
            var apiKey = _configuration["GoogleMaps:ApiKey"];
            var url = $"https://www.googleapis.com/geolocation/v1/geolocate?key={apiKey}";

            using (var httpClient = new HttpClient())
            {
                var response = await httpClient.PostAsync(url, new StringContent("{\"considerIp\": \"true\"}", Encoding.UTF8, "application/json"));
                
                // Log the response status code
                Console.WriteLine("Google Maps API Response Status: " + response.StatusCode);
                
                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    Console.WriteLine("Google Maps API Response: " + jsonResponse); // Log the full response
                    
                    // Deserialize the JSON response to extract latitude and longitude
                    var locationData = JsonSerializer.Deserialize<GeolocationResponse>(jsonResponse);
                    if (locationData != null && locationData.Location != null)
                    {
                        // Log the deserialized latitude and longitude
                        Console.WriteLine("Deserialized Latitude: " + locationData.Location.Latitude);
                        Console.WriteLine("Deserialized Longitude: " + locationData.Location.Longitude);
                        
                        return (locationData.Location.Latitude, locationData.Location.Longitude);
                    }
                }
                else
                {
                    // Log the error response if the request was not successful
                    var errorResponse = await response.Content.ReadAsStringAsync();
                    Console.WriteLine("Error Response: " + errorResponse);
                }
            }

            return null; // Return null if the API call fails
        }

        // Define a class to match the structure of the JSON response
        private class GeolocationResponse
        {
            [JsonPropertyName("location")]
            public Location? Location { get; set; }
        }

        private class Location
        {
            [JsonPropertyName("lat")]
            public double Latitude { get; set; }

            [JsonPropertyName("lng")]
            public double Longitude { get; set; }
        }
    }
} 