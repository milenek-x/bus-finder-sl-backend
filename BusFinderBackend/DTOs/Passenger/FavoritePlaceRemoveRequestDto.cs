namespace BusFinderBackend.DTOs.Passenger
{
    public class FavoritePlaceRemoveRequestDto
    {
        public string PlaceName { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
} 