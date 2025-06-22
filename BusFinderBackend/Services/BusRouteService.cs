using BusFinderBackend.Model;
using BusFinderBackend.Repositories;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace BusFinderBackend.Services
{
    public class BusRouteService
    {
        private readonly BusRouteRepository _busRouteRepository;
        private readonly IConfiguration _configuration;

        public BusRouteService(BusRouteRepository busRouteRepository, IConfiguration configuration)
        {
            _busRouteRepository = busRouteRepository;
            _configuration = configuration;
        }

        public Task<List<BusRoute>> GetAllBusRoutesAsync()
        {
            return _busRouteRepository.GetAllBusRoutesAsync();
        }

        public Task<BusRoute?> GetBusRouteByNumberAsync(string routeNumber)
        {
            return _busRouteRepository.GetBusRouteByNumberAsync(routeNumber);
        }

        public async Task<(bool Success, string? ErrorCode, string? ErrorMessage)> AddBusRouteAsync(BusRoute busRoute)
        {
            if (string.IsNullOrEmpty(busRoute.RouteNumber))
            {
                return (false, "NO_ROUTE_NUMBER", "Bus route number must be provided.");
            }

            // Here you can add any additional logic if needed, such as checking for duplicates

            await _busRouteRepository.AddBusRouteAsync(busRoute);
            return (true, null, null);
        }

        public Task UpdateBusRouteAsync(string routeNumber, BusRoute busRoute)
        {
            return _busRouteRepository.UpdateBusRouteAsync(routeNumber, busRoute);
        }

        public Task DeleteBusRouteAsync(string routeNumber)
        {
            return _busRouteRepository.DeleteBusRouteAsync(routeNumber);
        }

    }
}
