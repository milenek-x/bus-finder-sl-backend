using System.Collections.Generic;

namespace BusFinderBackend.DTOs.BusRoute
{
    public class BusRouteWithShiftsDto
    {
        public BusFinderBackend.Model.BusRoute? Route { get; set; }
        public List<BusFinderBackend.DTOs.BusShift.BusShiftDto>? Shifts { get; set; }
    }
} 