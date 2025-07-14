namespace BusFinderBackend.DTOs.Staff
{
    public class StaffResetPasswordRequestDto
    {
        public string? Email { get; set; }
        public string? NewPassword { get; set; }
    }
} 