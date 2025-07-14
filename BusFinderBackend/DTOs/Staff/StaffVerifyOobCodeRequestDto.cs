namespace BusFinderBackend.DTOs.Staff
{
    public class StaffVerifyOobCodeRequestDto
    {
        public string? Email { get; set; }
        public string? OobCode { get; set; }
    }
} 