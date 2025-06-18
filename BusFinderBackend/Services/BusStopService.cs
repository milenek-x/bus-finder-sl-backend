using BusFinderBackend.Firebase;
using BusFinderBackend.Model;
using BusFinderBackend.Repositories;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text.Json;

namespace BusFinderBackend.Services
{
    public class BusStopService
    {
        private readonly BusStopRepository _busStopRepository;
        private readonly IConfiguration _configuration;

        public BusStopService(BusStopRepository busStopRepository, IConfiguration configuration)
        {
            _busStopRepository = busStopRepository;
            _configuration = configuration;
        }

        public Task<List<BusStop>> GetAllBusStopsAsync()
        {
            return _busStopRepository.GetAllBusStopsAsync();
        }

        public Task<BusStop?> GetBusStopByNameAsync(string stopName)
        {
            return _busStopRepository.GetBusStopByNameAsync(stopName);
        }


        public async Task<(bool Success, string? ErrorCode, string? ErrorMessage)> AddBusStopAsync(BusStop busStop)
        {
            if (string.IsNullOrEmpty(busStop.StopName))
            {
                return (false, "NO_STOP_NAME", "Bus stop name must be provided.");
            }

            // Here you can add any additional logic if needed, such as checking for duplicates

            await _busStopRepository.AddBusStopAsync(busStop);
            return (true, null, null);
        }

        public Task UpdateBusStopAsync(string stopName, BusStop busStop)
        {
            return _busStopRepository.UpdateBusStopAsync(stopName, busStop);
        }

        public Task DeleteBusStopAsync(string stopName)
        {
            return _busStopRepository.DeleteBusStopAsync(stopName);
        }

        public async Task<List<BusStop>> SearchBusStopsUsingGoogleApiAsync(string name)
        {
            var apiKey = _configuration["GoogleMaps:ApiKey"];
            var url = $"https://maps.googleapis.com/maps/api/place/textsearch/json?query=Bus Stops near {name}&key={apiKey}&type=transit_station";

            using (var httpClient = new HttpClient())
            {
                var response = await httpClient.GetStringAsync(url);
                Console.WriteLine(response); // Log the full response for debugging

                var placesResponse = JsonSerializer.Deserialize<GooglePlacesResponse>(response);

                // Log only the results part
                if (placesResponse != null)
                {
                    Console.WriteLine(JsonSerializer.Serialize(placesResponse)); // Log the results
                }
                else
                {
                    Console.WriteLine("No results found.");
                }

                var busStops = new List<BusStop>();
                var uniqueBusStops = new HashSet<string>(); // To track unique bus stops

                if (placesResponse?.Results != null)
                {
                    foreach (var result in placesResponse.Results)
                    {
                        if (result?.Geometry?.Location != null)
                        {
                            // Create a unique identifier for the bus stop
                            var uniqueIdentifier = $"{result.Name}-{result.Geometry.Location.Lat}-{result.Geometry.Location.Lng}";

                            // Check if this bus stop is already added
                            if (uniqueBusStops.Add(uniqueIdentifier)) // Add returns false if the item already exists
                            {
                                busStops.Add(new BusStop
                                {
                                    StopName = result.Name,
                                    StopLatitude = result.Geometry.Location.Lat,
                                    StopLongitude = result.Geometry.Location.Lng
                                });
                            }
                        }
                    }
                }
                Console.WriteLine($"Added {busStops.Count} unique bus stops from Google Places API.");
                return busStops;
            }
        }

        public async Task<List<BusStop>> SearchBusStopsByPartialNameAsync(string partialName)
        {
            return await _busStopRepository.SearchBusStopsByPartialNameAsync(partialName);
        }
    }
}
