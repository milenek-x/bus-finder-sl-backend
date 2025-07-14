namespace BusFinderBackend.DTOs.Staff
{
    public class StaffPasswordUpdateRequestDto
    {
        public string? Email { get; set; }
        public string? OldPassword { get; set; }
        public string? NewPassword { get; set; }
    }
} 