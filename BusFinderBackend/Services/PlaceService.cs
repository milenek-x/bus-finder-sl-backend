using BusFinderBackend.Repositories;
using BusFinderBackend.Model;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace BusFinderBackend.Services
{
    public class PlaceService
    {
        private readonly PlaceRepository _placeRepository;
        private readonly IConfiguration _configuration;

        public PlaceService(PlaceRepository placeRepository, IConfiguration configuration)
        {
            _placeRepository = placeRepository;
            _configuration = configuration;
        }

        public Task<List<Place>> GetAllPlacesAsync()
        {
            return _placeRepository.GetAllPlacesAsync();
        }

        public Task<Place?> GetPlaceByIdAsync(string placeId)
        {
            return _placeRepository.GetPlaceByIdAsync(placeId);
        }

        public async Task<(bool Success, string? ErrorCode, string? ErrorMessage)> AddPlaceAsync(Place place)
        {
            if (string.IsNullOrEmpty(place.PlaceId))
            {
                place.PlaceId = await _placeRepository.GenerateNextPlaceIdAsync(); // Auto-generate PlaceId
            }

            await _placeRepository.AddPlaceAsync(place);
            return (true, null, null);
        }

        public Task UpdatePlaceAsync(string placeId, Place place)
        {
            return _placeRepository.UpdatePlaceAsync(placeId, place);
        }

        public Task DeletePlaceAsync(string placeId)
        {
            return _placeRepository.DeletePlaceAsync(placeId);
        }

        public async Task<List<Place>> SearchPlacesUsingGoogleApiAsync(string name)
        {
            var apiKey = _configuration["GoogleMaps:ApiKey"];
            var url = $"https://maps.googleapis.com/maps/api/place/textsearch/json?query=Tourist Attraction Places near {name}&key={apiKey}&type=point_of_interest";

            using (var httpClient = new HttpClient())
            {
                var response = await httpClient.GetStringAsync(url);
                var placesResponse = JsonSerializer.Deserialize<GooglePlacesResponse>(response);

                var places = new List<Place>();
                var uniquePlaces = new HashSet<string>();

                if (placesResponse?.Results != null)
                {
                    foreach (var result in placesResponse.Results)
                    {
                        var placeName = result?.Name ?? string.Empty;
                        var location = result?.Geometry?.Location;
                        if (!string.IsNullOrEmpty(placeName) && location != null)
                        {
                            var uniqueIdentifier = $"{placeName}-{location.Lat}-{location.Lng}";
                            if (uniquePlaces.Add(uniqueIdentifier))
                            {
                                places.Add(new Place
                                {
                                    PlaceName = placeName,
                                    Latitude = location.Lat,
                                    Longitude = location.Lng,
                                    LocationImage = (result?.Photos != null && result.Photos.Count > 0 && result.Photos[0]?.PhotoReference != null)
                                        ? $"https://maps.googleapis.com/maps/api/place/photo?maxwidth=400&photoreference={result.Photos[0]!.PhotoReference!}&key={apiKey}"
                                        : string.Empty
                                });
                            }
                        }
                    }
                }
                return places;
            }
        }

        public async Task<List<Place>> SearchPlacesByPartialNameAsync(string partialName)
        {
            var allPlaces = await _placeRepository.GetAllPlacesAsync();
            return allPlaces.FindAll(p => p.PlaceName.Contains(partialName, StringComparison.OrdinalIgnoreCase));
        }
    }
}
