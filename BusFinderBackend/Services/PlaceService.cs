using BusFinderBackend.Repositories;
using BusFinderBackend.Model;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BusFinderBackend.Services
{
    public class PlaceService
    {
        private readonly PlaceRepository _placeRepository;

        public PlaceService(PlaceRepository placeRepository)
        {
            _placeRepository = placeRepository;
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
    }
}
