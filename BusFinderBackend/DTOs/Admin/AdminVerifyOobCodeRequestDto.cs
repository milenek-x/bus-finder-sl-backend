namespace BusFinderBackend.DTOs.Admin
{
    public class AdminVerifyOobCodeRequestDto
    {
        public string? Email { get; set; }
        public string? OobCode { get; set; }
    }
} 