using BusFinderBackend.Model;
using BusFinderBackend.Repositories;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using BusFinderBackend.Services;
using BusFinderBackend.Model.DTOs;

namespace BusFinderBackend.Services
{
    public class BusRouteService
    {
        private readonly BusRouteRepository _busRouteRepository;
        private readonly IConfiguration _configuration;
        private readonly BusStopRepository _busStopRepository;
        private readonly BusShiftService _busShiftService;

        public BusRouteService(BusRouteRepository busRouteRepository, BusStopRepository busStopRepository, BusShiftService busShiftService, IConfiguration configuration)
        {
            _busRouteRepository = busRouteRepository ?? throw new ArgumentNullException(nameof(busRouteRepository));
            _busStopRepository = busStopRepository ?? throw new ArgumentNullException(nameof(busStopRepository));
            _busShiftService = busShiftService ?? throw new ArgumentNullException(nameof(busShiftService));
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

            // --- Add return route automatically ---
            if (busRoute.RouteStops != null && busRoute.RouteStops.Count > 1)
            {
                // Reverse the stops
                var reversedStops = new List<string>(busRoute.RouteStops);
                reversedStops.Reverse();

                // Reverse the route name (swap start and end)
                string? reversedName = null;
                if (!string.IsNullOrEmpty(busRoute.RouteName) && busRoute.RouteName.Contains("-"))
                {
                    var parts = busRoute.RouteName.Split('-');
                    if (parts.Length == 2)
                    {
                        reversedName = parts[1].Trim() + " - " + parts[0].Trim();
                    }
                    else
                    {
                        reversedName = busRoute.RouteName + " (Return)";
                    }
                }
                else
                {
                    reversedName = busRoute.RouteName + " (Return)";
                }

                var returnRoute = new BusRoute
                {
                    RouteNumber = busRoute.RouteNumber + "R",
                    RouteName = reversedName,
                    RouteStops = reversedStops
                };

                // Optional: Check if return route already exists
                var existingReturnRoute = await _busRouteRepository.GetBusRouteByNumberAsync(returnRoute.RouteNumber);
                if (existingReturnRoute == null)
                {
                    await _busRouteRepository.AddBusRouteAsync(returnRoute);
                }
            }
            // --- End add return route ---

            return (true, null, null);
        }

        public async Task UpdateBusRouteAsync(string routeNumber, BusRoute busRoute)
        {
            await _busRouteRepository.UpdateBusRouteAsync(routeNumber, busRoute);

            // --- Update return route automatically ---
            if (busRoute.RouteStops != null && busRoute.RouteStops.Count > 1)
            {
                // Reverse the stops
                var reversedStops = new List<string>(busRoute.RouteStops);
                reversedStops.Reverse();

                // Reverse the route name (swap start and end)
                string? reversedName = null;
                if (!string.IsNullOrEmpty(busRoute.RouteName) && busRoute.RouteName.Contains("-"))
                {
                    var parts = busRoute.RouteName.Split('-');
                    if (parts.Length == 2)
                    {
                        reversedName = parts[1].Trim() + " - " + parts[0].Trim();
                    }
                    else
                    {
                        reversedName = busRoute.RouteName + " (Return)";
                    }
                }
                else
                {
                    reversedName = busRoute.RouteName + " (Return)";
                }

                var returnRoute = new BusRoute
                {
                    RouteNumber = busRoute.RouteNumber + "R",
                    RouteName = reversedName,
                    RouteStops = reversedStops
                };

                await _busRouteRepository.UpdateBusRouteAsync(returnRoute.RouteNumber, returnRoute);
            }
            // --- End update return route ---
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

        public async Task<List<BusRouteWithShiftsDto>> GetBusRoutesByStopsAsync(string startingPoint, string endingPoint, string date, string time)
        {
            var allBusRoutes = await GetAllBusRoutesAsync();
            var matchingRoutes = new List<BusRouteWithShiftsDto>();

            foreach (var route in allBusRoutes)
            {
                if (route.RouteStops != null && route.RouteStops.Contains(startingPoint) && route.RouteStops.Contains(endingPoint) && !string.IsNullOrEmpty(route.RouteNumber))
                {
                    var shifts = await _busShiftService.GetBusShiftsByRouteNumberAsync(route.RouteNumber!, date, time);
                    matchingRoutes.Add(new BusRouteWithShiftsDto
                    {
                        Route = route,
                        Shifts = shifts
                    });
                }
            }

            return matchingRoutes;
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
