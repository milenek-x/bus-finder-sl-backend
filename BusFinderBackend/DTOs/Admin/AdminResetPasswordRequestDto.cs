namespace BusFinderBackend.DTOs.Admin
{
    public class AdminResetPasswordRequestDto
    {
        public string? Email { get; set; }
        public string? NewPassword { get; set; }
    }
} 