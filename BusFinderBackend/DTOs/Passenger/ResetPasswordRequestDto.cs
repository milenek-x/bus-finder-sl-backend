namespace BusFinderBackend.DTOs.Passenger
{
    public class ResetPasswordRequestDto
    {
        public string Email { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }
} 