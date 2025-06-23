using BusFinderBackend.Model;
using BusFinderBackend.Repositories;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BusFinderBackend.Services
{
    public class BusService
    {
        private readonly BusRepository _busRepository;
        private readonly StaffService _staffService;
        private readonly BusRouteRepository _busRouteRepository;

        public BusService(BusRepository busRepository, StaffService staffService, BusRouteRepository busRouteRepository)
        {
            _busRepository = busRepository;
            _staffService = staffService;
            _busRouteRepository = busRouteRepository;
        }

        public async Task<List<Bus>> GetAllBusesAsync()
        {
            return await _busRepository.GetAllBusesAsync();
        }

        public async Task<Bus?> GetBusByNumberPlateAsync(string numberPlate)
        {
            return await _busRepository.GetBusByNumberPlateAsync(numberPlate);
        }

        public async Task AddBusAsync(Bus bus)
        {
            // Check if the staff ID is valid
            if (string.IsNullOrEmpty(bus.StaffID))
            {
                throw new ArgumentException("StaffID cannot be null or empty.");
            }

            var staff = await _staffService.GetStaffByIdAsync(bus.StaffID);
            if (staff == null)
            {
                throw new ArgumentException("Invalid StaffID: The specified staff does not exist.");
            }

            // Check if the bus route number is valid
            if (string.IsNullOrEmpty(bus.BusRouteNumber))
            {
                throw new ArgumentException("BusRouteNumber cannot be null or empty.");
            }

            var busRoute = await _busRepository.GetBusRouteByNumberAsync(bus.BusRouteNumber);
            if (busRoute == null)
            {
                throw new ArgumentException("Invalid BusRouteNumber: The specified route does not exist.");
            }

            // Set default values for unspecified properties
            bus.BusCapacity = false; // Default value
            bus.SosStatus = false; // Default value
            bus.CurrentLocationLatitude = 0.0; // Default value
            bus.CurrentLocationLongitude = 0.0; // Default value

            await _busRepository.AddBusAsync(bus);
        }

        public async Task UpdateBusAsync(string numberPlate, Bus bus)
        {
            // Check if the bus exists
            var existingBus = await _busRepository.GetBusByNumberPlateAsync(numberPlate);
            if (existingBus == null)
            {
                throw new ArgumentException("Bus not found.");
            }

            // Check if the staff ID is valid
            if (string.IsNullOrEmpty(bus.StaffID))
            {
                throw new ArgumentException("StaffID cannot be null or empty.");
            }

            var staff = await _staffService.GetStaffByIdAsync(bus.StaffID);
            if (staff == null)
            {
                throw new ArgumentException("Invalid StaffID: The specified staff does not exist.");
            }

            // Check if the bus route number is valid
            if (string.IsNullOrEmpty(bus.BusRouteNumber))
            {
                throw new ArgumentException("BusRouteNumber cannot be null or empty.");
            }

            var busRoute = await _busRepository.GetBusRouteByNumberAsync(bus.BusRouteNumber);
            if (busRoute == null)
            {
                throw new ArgumentException("Invalid BusRouteNumber: The specified route does not exist.");
            }

            // Update the bus properties
            existingBus.BusType = bus.BusType;
            existingBus.StaffID = bus.StaffID;
            existingBus.BusRouteNumber = bus.BusRouteNumber;

            await _busRepository.UpdateBusAsync(numberPlate, existingBus);
        }

        public async Task DeleteBusAsync(string numberPlate)
        {
            // Additional business logic can be added here if needed
            await _busRepository.DeleteBusAsync(numberPlate);
        }

        public async Task UpdateBusCapacityAsync(string numberPlate, bool busCapacity)
        {
            await _busRepository.UpdateBusCapacityAsync(numberPlate, busCapacity);
        }

        public async Task UpdateSosStatusAsync(string numberPlate, bool sosStatus)
        {
            await _busRepository.UpdateSosStatusAsync(numberPlate, sosStatus);
        }

        public async Task UpdateCurrentLocationAsync(string numberPlate, double latitude, double longitude)
        {
            await _busRepository.UpdateCurrentLocationAsync(numberPlate, latitude, longitude);
        }
    }
} 