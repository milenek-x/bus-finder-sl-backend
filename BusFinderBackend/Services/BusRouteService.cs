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

            // Calculate and set the distance for the main route
            busRoute.RouteDistance = await CalculateRouteDistanceAsync(busRoute.RouteStops);

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
                    RouteStops = reversedStops,
                    RouteDistance = await CalculateRouteDistanceAsync(reversedStops)
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
            // Calculate and set the distance for the main route
            busRoute.RouteDistance = await CalculateRouteDistanceAsync(busRoute.RouteStops);

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
                    RouteStops = reversedStops,
                    RouteDistance = await CalculateRouteDistanceAsync(reversedStops)
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

        /// <summary>
        /// Calculates the total road distance (in kilometers) for a bus route by connecting all stops in order using the Google Maps Directions API.
        /// </summary>
        /// <param name="routeStops">List of stop names in order</param>
        /// <returns>Total distance in kilometers, or null if calculation fails</returns>
        public async Task<double?> CalculateRouteDistanceAsync(List<string>? routeStops)
        {
            if (routeStops == null || routeStops.Count < 2)
                return null;

            // Resolve coordinates for all stops
            var coordinates = new List<(double lat, double lng)>();
            foreach (var stopName in routeStops)
            {
                var stop = await _busStopRepository.GetBusStopByNameAsync(stopName);
                if (stop == null)
                    return null; // If any stop is missing, fail gracefully
                coordinates.Add((stop.StopLatitude, stop.StopLongitude));
            }

            // Build Directions API request
            var apiKey = _configuration["GoogleMaps:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
                return null;

            var origin = $"{coordinates[0].lat},{coordinates[0].lng}";
            var destination = $"{coordinates[^1].lat},{coordinates[^1].lng}";
            var waypoints = coordinates.Count > 2
                ? "waypoints=" + string.Join("|", coordinates.GetRange(1, coordinates.Count - 2).ConvertAll(c => $"{c.lat},{c.lng}"))
                : string.Empty;

            var url = $"https://maps.googleapis.com/maps/api/directions/json?origin={origin}&destination={destination}" +
                      (waypoints != string.Empty ? $"&{waypoints}" : "") +
                      $"&key={apiKey}";

            using var httpClient = new HttpClient();
            var response = await httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.TryGetProperty("routes", out var routes) && routes.GetArrayLength() > 0)
            {
                var legs = routes[0].GetProperty("legs");
                double totalMeters = 0;
                foreach (var leg in legs.EnumerateArray())
                {
                    if (leg.TryGetProperty("distance", out var distance) && distance.TryGetProperty("value", out var value))
                    {
                        totalMeters += value.GetDouble();
                    }
                }
                return totalMeters / 1000.0; // Convert to kilometers
            }
            return null;
        }
    }
}
