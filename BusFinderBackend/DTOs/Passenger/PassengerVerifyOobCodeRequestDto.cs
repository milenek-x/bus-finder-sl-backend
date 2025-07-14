namespace BusFinderBackend.DTOs.Passenger
{
    public class PassengerVerifyOobCodeRequestDto
    {
        public string? Email { get; set; }
        public string? OobCode { get; set; }
    }
} 