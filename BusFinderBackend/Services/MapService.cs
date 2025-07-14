using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using BusFinderBackend.Services; // Ensure this is included for BusService and BusRouteService

namespace BusFinderBackend.Services
{
    public class MapService
    {
        private readonly IConfiguration _configuration;
        private readonly BusService _busService; // Add BusService
        private readonly BusRouteService _busRouteService; // Add BusRouteService
        private readonly PassengerService _passengerService; // Add PassengerService
        

        public MapService(IConfiguration configuration, BusService busService, BusRouteService busRouteService, PassengerService passengerService)
        {
            _configuration = configuration;
            _busService = busService; // Initialize BusService
            _busRouteService = busRouteService; // Initialize BusRouteService
            _passengerService = passengerService; // Initialize PassengerService
        }

        // Method to get initial camera position
        private object GetInitialCameraPosition()
        {
            return new
            {
                latitude = 6.9271,
                longitude = 79.8612,
                zoom = 12.0,
                bearing = 0.0,
                tilt = 0
            };
        }

        // Method to get common map options
        private object GetCommonMapOptions()
        {
            return new
            {
                mapType = "roadmap",
                zoomControlsEnabled = true,
                compassEnabled = true,
                myLocationButtonEnabled = true,
                trafficEnabled = true,
                indoorEnabled = false,
                rotateGesturesEnabled = true,
                scrollGesturesEnabled = true,
                tiltGesturesEnabled = true,
                zoomGesturesEnabled = true,
                styles = new List<object>
                {
                    new { featureType = "poi", stylers = new List<object> { new { visibility = "off" } } },
                    new { featureType = "transit", stylers = new List<object> { new { visibility = "off" } } },
                    new { featureType = "road.highway", elementType = "labels", stylers = new List<object> { new { visibility = "off" } } }
                }
            };
        }

        // Method to get layers
        private List<object> GetLayers(bool includeAllBusStops, bool includeAllBusRoutes, bool includeLiveAllBusLocations, bool includeLivePassengerLocation, bool includeSingleBusRoute, bool includeSingleBusLocation, bool includeAllBusesInSingleRoute, bool includeFamousPlaces, string? busRoute = null, string? bus = null, string? passenger = null)
        {
            var layers = new List<object>();

            if (includeAllBusStops)
            {
                layers.Add(new
                {
                    id = "busStopsLayer",
                    type = "geojson",
                    sourceUrl = "http://localhost:5176/api/busstop/geojson",
                    renderOptions = new
                    {
                        markerIconUrl = "https://placehold.co/32x32/FF0000/FFFFFF?text=ST",
                        clusterMarkers = true
                    }
                });
            }

            if (includeAllBusRoutes)
            {
                layers.Add(new
                {
                    id = "busRoutesLayer",
                    type = "geojson",
                    sourceUrl = "http://localhost:5176/api/busroute/geojson",
                    renderOptions = new
                    {
                        strokeColor = "#2C44BB",
                        strokeWidth = 5,
                        strokeOpacity = 0.7
                    }
                });
            }

            if (includeLiveAllBusLocations)
            {
                layers.Add(new
                {
                    id = "liveBusLocationsLayer",
                    type = "realtime",
                    signalRHubUrl = "/busHub",
                    renderOptions = new
                    {
                        markerIconUrl = "https://placehold.co/32x32/00FF00/000000?text=BUS",
                        animateMovement = true
                    }
                });
            }

            if (includeLivePassengerLocation && !string.IsNullOrEmpty(passenger))
            {
                layers.Add(new
                {
                    id = "passengerLayer",
                    type = "realtime",
                    signalRHubUrl = $"/passengerHub/{passenger}",
                    renderOptions = new
                    {
                        markerIconUrl = "https://placehold.co/32x32/000000/FFFFFF?text=ME",
                        animateMovement = true,
                        showLabel = true,
                        labelTemplate = "{passengerName}",
                        labelOffset = new int[] { 0, -35 }
                    }
                });
            }

            if (includeSingleBusRoute && !string.IsNullOrEmpty(busRoute))
            {
                layers.Add(new
                {
                    id = "singleBusRouteLayer",
                    type = "geojson",
                    sourceUrl = $"http://localhost:5176/api/busroute/single/geojson/{busRoute}",
                    renderOptions = new
                    {
                        strokeColor = "#FF0000",
                        strokeWidth = 3,
                        strokeOpacity = 0.8
                    }
                });
            }

            if (includeSingleBusLocation && !string.IsNullOrEmpty(bus))
            {
                layers.Add(new
                {
                    id = "singleBusLayer",
                    type = "realtime",
                    signalRHubUrl = "/busHub",
                    renderOptions = new
                    {
                        markerIconUrl = "https://placehold.co/32x32/0000FF/FFFFFF?text=BUS",
                        animateMovement = true
                    }
                });
            }

            if (includeAllBusesInSingleRoute)
            {
                layers.Add(new
                {
                    id = "busesInRouteLayer",
                    type = "realtime",
                    signalRHubUrl = "/busesInRouteHub",
                    renderOptions = new
                    {
                        markerIconUrl = "https://placehold.co/32x32/FFFF00/000000?text=BUS",
                        animateMovement = true
                    }
                });
            }

            if (includeFamousPlaces)
            {
                layers.Add(new
                {
                    id = "famousPlacesLayer",
                    type = "geojson",
                    sourceUrl = "http://localhost:5176/api/famousplaces/geojson",
                    renderOptions = new
                    {
                        markerIconUrl = "https://placehold.co/32x32/0000FF/FFFFFF?text=FP",
                        clusterMarkers = true
                    }
                });
            }

            return layers;
        }

        // Method to get map configuration options
        public object GetAllMapConfiguration()
        {
            return new
            {
                googleMapsApiKey = _configuration["GoogleMaps:ApiKey"],
                initialCameraPosition = GetInitialCameraPosition(),
                mapOptions = GetCommonMapOptions(),
                layers = GetLayers(true, true, true, true, false, false, false, false) // Include all layers except single bus route, single bus, and buses in a single route
            };
        }

        public object GetAdminViewAllBusConfiguration()
        {
            return new
            {
                googleMapsApiKey = _configuration["GoogleMaps:ApiKey"],
                initialCameraPosition = GetInitialCameraPosition(),
                mapOptions = GetCommonMapOptions(),
                layers = GetLayers(false, false, true, false, false, false, false, false) // Include live bus locations, single bus route, single bus, and buses in a single route
            };
        }

        public object GetStaffViewLiveBusShiftConfiguration(string busRoute, string bus)
        {
            // Fetch the specific bus and bus route details from the repository or service
            var busDetails = _busService.GetBusByNumberPlateAsync(bus).Result; // Assuming this method exists
            var busRouteDetails = _busRouteService.GetBusRouteByNumberAsync(busRoute).Result; // Assuming this method exists

            // Check if the bus and bus route exist
            if (busDetails == null)
            {
                // Handle the error for bus not found
                return new
                {
                    error = "Bus not found.",
                    message = $"No bus found with the number plate: {bus}"
                };
            }

            if (busRouteDetails == null)
            {
                // Handle the error for bus route not found
                return new
                {
                    error = "Bus route not found.",
                    message = $"No bus route found with the number: {busRoute}"
                };
            }

            return new
            {
                googleMapsApiKey = _configuration["GoogleMaps:ApiKey"],
                initialCameraPosition = GetInitialCameraPosition(),
                mapOptions = GetCommonMapOptions(),
                layers = GetLayers(true, false, false, false, true, true, false, false, busRoute, bus) // Pass the busRoute and bus
            };
        }

        public object GetPassengerViewLiveBusRouteConfiguration(string busRoute, string bus, string passenger)
        {
            // Fetch the specific bus and bus route details from the repository or service
            var busDetails = _busService.GetBusByNumberPlateAsync(bus).Result; // Assuming this method exists
            var busRouteDetails = _busRouteService.GetBusRouteByNumberAsync(busRoute).Result; // Assuming this method exists
            var passengerDetails = _passengerService.GetPassengerByIdAsync(passenger).Result; // Assuming this method exists

            // Check if the bus, bus route, and passenger exist
            if (busDetails == null)
            {
                return new
                {
                    error = "Bus not found.",
                    message = $"No bus found with the number plate: {bus}"
                };
            }

            if (busRouteDetails == null)
            {
                return new
                {
                    error = "Bus route not found.",
                    message = $"No bus route found with the number: {busRoute}"
                };
            }

            if (passengerDetails == null)
            {
                return new
                {
                    error = "Passenger not found.",
                    message = $"No passenger found with the ID: {passenger}"
                };
            }

            return new
            {
                googleMapsApiKey = _configuration["GoogleMaps:ApiKey"],
                initialCameraPosition = GetInitialCameraPosition(),
                mapOptions = GetCommonMapOptions(),
                layers = GetLayers(true, false, false, true, true, true, false, true, busRoute, bus, passenger) // Pass the busRoute, bus, and passenger
            };
        }

        public object GetPassengerViewLiveLocation(string passenger)
        {
            // Fetch the passenger details from the service
            var passengerDetails = _passengerService.GetPassengerByIdAsync(passenger).Result; // Assuming this method exists

            // Check if the passenger exists
            if (passengerDetails == null)
            {
                return new
                {
                    error = "Passenger not found.",
                    message = $"No passenger found with the ID: {passenger}"
                };
            }

            return new
            {
                googleMapsApiKey = _configuration["GoogleMaps:ApiKey"],
                initialCameraPosition = GetInitialCameraPosition(),
                mapOptions = GetCommonMapOptions(),
                layers = GetLayers(false, false, false, true, false, false, false, false, null, null, passenger) // Only include live passenger location
            };
        }
    }
}