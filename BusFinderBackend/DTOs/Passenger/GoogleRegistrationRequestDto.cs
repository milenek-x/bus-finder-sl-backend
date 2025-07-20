using BusFinderBackend.Model;

namespace BusFinderBackend.DTOs.Passenger
{
    public class GoogleRegistrationRequestDto
    {
        public string IdToken { get; set; } = string.Empty;
        public Passenger Passenger { get; set; } = new Passenger();
    }
} 