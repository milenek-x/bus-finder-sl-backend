namespace BusFinderBackend.DTOs.Admin
{
    public class AdminPasswordUpdateRequestDto
    {
        public string? Email { get; set; }
        public string? OldPassword { get; set; }
        public string? NewPassword { get; set; }
    }
} 