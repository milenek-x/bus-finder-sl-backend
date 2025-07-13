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
        private readonly BusStopRepository _busStopRepository;

        public BusRouteService(BusRouteRepository busRouteRepository, BusStopRepository busStopRepository, IConfiguration configuration)
        {
            _busRouteRepository = busRouteRepository ?? throw new ArgumentNullException(nameof(busRouteRepository));
            _busStopRepository = busStopRepository ?? throw new ArgumentNullException(nameof(busStopRepository));
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

        public async Task<string> GetGeoJSONBusRoutesAsync()
        {
            var busRoutes = await GetAllBusRoutesAsync();
            var geoJson = new
            {
                type = "FeatureCollection",
                features = new List<object>()
            };

            foreach (var route in busRoutes)
            {
                var feature = new
                {
                    type = "Feature",
                    geometry = new
                    {
                        type = "LineString",
                        coordinates = await GetRouteCoordinatesAsync(route.RouteStops)
                    },
                    properties = new
                    {
                        name = route.RouteName,
                        id = route.RouteNumber // or any unique identifier
                    }
                };
                geoJson.features.Add(feature);
            }

            return JsonSerializer.Serialize(geoJson);
        }

        public async Task<string?> GetGeoJSONBusRouteAsync(string routeNumber)
        {
            var busRoute = await GetBusRouteByNumberAsync(routeNumber);
            if (busRoute == null)
            {
                return null;
            }

            var geoJson = new
            {
                type = "FeatureCollection",
                features = new List<object>
                {
                    new
                    {
                        type = "Feature",
                        geometry = new
                        {
                            type = "LineString",
                            coordinates = await GetRouteCoordinatesAsync(busRoute.RouteStops)
                        },
                        properties = new
                        {
                            name = busRoute.RouteName,
                            id = busRoute.RouteNumber
                        }
                    }
                }
            };

            return JsonSerializer.Serialize(geoJson);
        }

        private async Task<List<double[]>> GetRouteCoordinatesAsync(List<string>? routeStops)
        {
            if (routeStops == null) return new List<double[]>(); // Handle null case
            var coordinates = new List<double[]>();
            foreach (var stop in routeStops)
            {
                var busStop = await _busStopRepository.GetBusStopByNameAsync(stop);
                if (busStop != null)
                {
                    coordinates.Add(new double[] { busStop.StopLongitude, busStop.StopLatitude });
                }
            }
            return coordinates;
        }
    }
}
