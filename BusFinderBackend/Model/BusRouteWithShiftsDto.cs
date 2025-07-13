using System.Collections.Generic;

namespace BusFinderBackend.Model.DTOs
{
    public class BusRouteWithShiftsDto
    {
        public BusRoute? Route { get; set; }
        public List<BusFinderBackend.DTO.BusShiftDto>? Shifts { get; set; }
    }
} 