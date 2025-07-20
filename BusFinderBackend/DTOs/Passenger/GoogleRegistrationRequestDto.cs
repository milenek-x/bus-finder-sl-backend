using BusFinderBackend.Model;

namespace BusFinderBackend.DTOs.Passenger
{
    public class GoogleRegistrationRequestDto
    {
        public string IdToken { get; set; } = string.Empty;
        public BusFinderBackend.Model.Passenger Passenger { get; set; } = new BusFinderBackend.Model.Passenger();
    }
} 