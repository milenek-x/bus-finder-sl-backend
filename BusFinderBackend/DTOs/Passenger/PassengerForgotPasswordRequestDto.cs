namespace BusFinderBackend.DTOs.Passenger
{
    public class PassengerForgotPasswordRequestDto
    {
        public string? Email { get; set; }
        public string? NewPassword { get; set; }
    }
} 