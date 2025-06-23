using BusFinderBackend.Model;
using BusFinderBackend.Repositories;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BusFinderBackend.Services
{
    public class BusShiftService
    {
        private readonly BusShiftRepository _busShiftRepository;
        private readonly BusRouteRepository _busRouteRepository;

        public BusShiftService(BusShiftRepository busShiftRepository, BusRouteRepository busRouteRepository)
        {
            _busShiftRepository = busShiftRepository;
            _busRouteRepository = busRouteRepository;
        }

        public async Task<List<BusShift>> GetAllBusShiftsAsync()
        {
            return await _busShiftRepository.GetAllBusShiftsAsync();
        }

        public async Task<BusShift?> GetBusShiftByIdAsync(string shiftId)
        {
            return await _busShiftRepository.GetBusShiftByIdAsync(shiftId);
        }

        public async Task AddBusShiftAsync(BusShift busShift)
        {
            if (string.IsNullOrEmpty(busShift.RouteNo))
            {
                throw new ArgumentException("RouteNo cannot be null or empty.");
            }

            // Check if the route number is valid
            var busRoute = await _busRouteRepository.GetBusRouteByNumberAsync(busShift.RouteNo);
            if (busRoute == null)
            {
                throw new ArgumentException("Invalid RouteNo: The specified route does not exist.");
            }

            if (string.IsNullOrEmpty(busShift.ShiftId))
            {
                busShift.ShiftId = await _busShiftRepository.GenerateNextShiftIdAsync();
            }

            await _busShiftRepository.AddBusShiftAsync(busShift);
        }

        public async Task UpdateBusShiftAsync(string shiftId, BusShift busShift)
        {
            if (string.IsNullOrEmpty(busShift.RouteNo))
            {
                throw new ArgumentException("RouteNo cannot be null or empty.");
            }

            // Check if the route number is valid
            var busRoute = await _busRouteRepository.GetBusRouteByNumberAsync(busShift.RouteNo);
            if (busRoute == null)
            {
                throw new ArgumentException("Invalid RouteNo: The specified route does not exist.");
            }

            await _busShiftRepository.UpdateBusShiftAsync(shiftId, busShift);
        }

        public async Task DeleteBusShiftAsync(string shiftId)
        {
            await _busShiftRepository.DeleteBusShiftAsync(shiftId);
        }
    }
} 