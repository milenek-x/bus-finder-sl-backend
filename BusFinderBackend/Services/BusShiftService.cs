using BusFinderBackend.Model;
using BusFinderBackend.Repositories;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System;
using BusFinderBackend.DTO; // Update the using directive
using BusFinderBackend.Services; // Added this using directive for NotificationService

namespace BusFinderBackend.Services
{
    public class BusShiftService
    {
        private readonly BusShiftRepository _busShiftRepository;
        private readonly BusRouteRepository _busRouteRepository;
        private readonly NotificationService _notificationService;

        public BusShiftService(BusShiftRepository busShiftRepository, BusRouteRepository busRouteRepository, NotificationService notificationService)
        {
            _busShiftRepository = busShiftRepository;
            _busRouteRepository = busRouteRepository;
            _notificationService = notificationService;
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
            // Notify staff group
            await _notificationService.NotifyGroupAsync("staff", $"A new shift has been created: {busShift.ShiftId}");
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

        public async Task RemoveNormalShiftAsync(string shiftId)
        {
            var busShift = await _busShiftRepository.GetBusShiftByIdAsync(shiftId);
            if (busShift == null)
                throw new ArgumentException($"No BusShift found with ShiftId {shiftId}");
            busShift.Normal = null;
            await _busShiftRepository.UpdateBusShiftAsync(shiftId, busShift);
        }

        public async Task RemoveReverseShiftAsync(string shiftId)
        {
            var busShift = await _busShiftRepository.GetBusShiftByIdAsync(shiftId);
            if (busShift == null)
                throw new ArgumentException($"No BusShift found with ShiftId {shiftId}");
            busShift.Reverse = null;
            await _busShiftRepository.UpdateBusShiftAsync(shiftId, busShift);
        }

        public async Task DeleteBusShiftAsync(string shiftId)
        {
            await _busShiftRepository.DeleteBusShiftAsync(shiftId);
        }

        public async Task<List<DTOs.BusShift.BusShiftDto>> GetBusShiftsByRouteNumberAsync(string routeNumber, string date, string time)
        {
            var allBusShifts = await GetAllBusShiftsAsync();
            bool isReverse = routeNumber.Contains("R");
            string routeNoForComparison = isReverse ? routeNumber.Replace("R", "") : routeNumber;

            // Parse input date and time into DateTime
            if (!DateTime.TryParse($"{date} {time}", out var inputDateTime))
                return new List<DTOs.BusShift.BusShiftDto>();

            var matchingShifts = allBusShifts
                .Where(shift => shift.RouteNo == routeNoForComparison)
                .Select(shift => new
                {
                    Shift = shift,
                    Details = isReverse ? shift.Reverse : shift.Normal
                })
                .Where(x => x.Details != null)
                .Where(x =>
                {
                    // Date window filter
                    if (!DateTime.TryParse(x.Details!.Date, out var shiftDate))
                        return false;
                    var twoDaysFromInput = inputDateTime.Date.AddDays(2);
                    if (shiftDate.Date < inputDateTime.Date || shiftDate.Date > twoDaysFromInput.Date)
                        return false;

                    // Future shift filter
                    if (!DateTime.TryParse($"{x.Details!.Date} {x.Details!.EndTime}", out var shiftEndDateTime))
                        return false;
                    return shiftEndDateTime > inputDateTime;
                })
                .Select(x => new DTOs.BusShift.BusShiftDto
                {
                    ShiftId = x.Shift.ShiftId,
                    RouteNo = x.Shift.RouteNo,
                    NumberPlate = x.Shift.NumberPlate,
                    Normal = !isReverse ? new DTOs.BusShift.BusShiftNormalDto
                    {
                        StartTime = x.Details!.StartTime,
                        EndTime = x.Details!.EndTime,
                        Date = x.Details!.Date
                    } : null,
                    Reverse = isReverse ? new DTOs.BusShift.BusShiftReverseDto
                    {
                        StartTime = x.Details!.StartTime,
                        EndTime = x.Details!.EndTime,
                        Date = x.Details!.Date
                    } : null
                })
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