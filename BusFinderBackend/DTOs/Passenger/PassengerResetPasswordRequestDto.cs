namespace BusFinderBackend.DTOs.Passenger
{
    public class PassengerResetPasswordRequestDto
    {
        public string? Email { get; set; }
        public string? NewPassword { get; set; }
    }
} 