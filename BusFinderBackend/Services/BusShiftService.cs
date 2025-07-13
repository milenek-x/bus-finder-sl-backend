using BusFinderBackend.Model;
using BusFinderBackend.Repositories;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System;
using BusFinderBackend.DTO; // Update the using directive

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

            // Handle Number Plate
            if (string.IsNullOrEmpty(busShift.NumberPlate))
            {
                throw new ArgumentException("Number Plate cannot be null or empty.");
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

        public async Task<List<BusShiftDto>> GetBusShiftsByRouteNumberAsync(string routeNumber, string date, string time)
        {
            var allBusShifts = await GetAllBusShiftsAsync();
            var matchingShifts = allBusShifts
                .Where(shift => shift.RouteNo == routeNumber && IsWithinFutureDate(shift.Date ?? "", date))
                .Select(shift => new BusShiftDto
                {
                    StartTime = shift.StartTime ?? "Unknown", // Handle possible null
                    EndTime = shift.EndTime ?? "Unknown", // Handle possible null
                    TravelTime = CalculateTravelTime(shift.StartTime, shift.EndTime),
                    Date = shift.Date // Map the Date property
                })
                .Where(shift => IsFutureShift(shift.EndTime ?? "Unknown", shift.Date ?? "", date, time))
                .ToList();
            return matchingShifts;
        }

        private TimeSpan CalculateTravelTime(string? startTime, string? endTime)
        {
            if (DateTime.TryParse(startTime, out var start) && DateTime.TryParse(endTime, out var end))
            {
                return end - start;
            }
            return TimeSpan.Zero;
        }

        private bool IsFutureShift(string endTime, string shiftDate, string inputDate, string inputTime)
        {
            // Parse the input date and time
            var inputDateTimeString = $"{inputDate} {inputTime}";
            if (!DateTime.TryParse(inputDateTimeString, out var inputDateTime))
            {
                return false;
            }

            // Parse the shift end date and time
            var shiftEndDateTimeString = $"{shiftDate} {endTime}";
            if (!DateTime.TryParse(shiftEndDateTimeString, out var shiftEndDateTime))
            {
                return false;
            }

            // Check if shift end time is after the input time
            return shiftEndDateTime > inputDateTime;
        }

        private bool IsWithinFutureDate(string shiftDate, string inputDate)
        {
            if (!DateTime.TryParse(shiftDate, out var shiftDateTime) || !DateTime.TryParse(inputDate, out var inputDateTime))
            {
                return false;
            }

            var twoDaysFromInput = inputDateTime.AddDays(2);
            return shiftDateTime.Date >= inputDateTime.Date && shiftDateTime.Date <= twoDaysFromInput.Date;
        }
    }
} 