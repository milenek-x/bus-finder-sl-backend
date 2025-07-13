namespace BusFinderBackend.DTO
{
    public class BusShiftDto
    {
        public string? StartTime { get; set; }
        public string? EndTime { get; set; }
        public TimeSpan TravelTime { get; set; }
        public string? Date { get; set; }
    }
} 