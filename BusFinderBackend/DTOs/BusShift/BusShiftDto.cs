using System;

namespace BusFinderBackend.DTOs.BusShift
{
    public class BusShiftDto
    {
        public string? ShiftId { get; set; }
        public BusShiftNormalDto? Normal { get; set; }
        public BusShiftReverseDto? Reverse { get; set; }
        public string? RouteNo { get; set; }
        public string? NumberPlate { get; set; }
    }
} 