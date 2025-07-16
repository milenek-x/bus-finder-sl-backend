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
        private List<object> GetLayers(bool includeAllBusStops, bool includeAllBusRoutes, bool includeLiveAllBusLocations, bool includeLivePassengerLocation, bool includeAllBusesInSingleRoute, bool includeFamousPlaces)
        {
            var layers = new List<object>();

            if (includeAllBusStops)
            {
                layers.Add(new
                {
                    id = "busStopsLayer",
                    type = "geojson",
                    sourceUrl = "https://bus-finder-sl-a7c6a549fbb1.herokuapp.com/api/busstop/geojson",
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
                    sourceUrl = "https://bus-finder-sl-a7c6a549fbb1.herokuapp.com/api/busroute/geojson",
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
                    signalRHubUrl = "https://bus-finder-sl-a7c6a549fbb1.herokuapp.com/busHub",
                    renderOptions = new
                    {
                        markerIconUrl = "https://placehold.co/32x32/00FF00/000000?text=BUS",
                        animateMovement = true
                    }
                });
            }

            if (includeLivePassengerLocation)
            {
                layers.Add(new
                {
                    id = "passengerLayer",
                    type = "realtime",
                    signalRHubUrl = "https://bus-finder-sl-a7c6a549fbb1.herokuapp.com/passengerHub",
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



            if (includeAllBusesInSingleRoute)
            {
                layers.Add(new
                {
                    id = "busesInRouteLayer",
                    type = "realtime",
                    signalRHubUrl = "https://bus-finder-sl-a7c6a549fbb1.herokuapp.com/busesInRouteHub",
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
                    sourceUrl = "https://bus-finder-sl-a7c6a549fbb1.herokuapp.com/api/famousplaces/geojson",
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
                layers = GetLayers(true, true, true, true, false, false) // Include all layers except buses in a single route
            };
        }

        public object GetAdminViewAllBusConfiguration()
        {
            return new
            {
                googleMapsApiKey = _configuration["GoogleMaps:ApiKey"],
                initialCameraPosition = GetInitialCameraPosition(),
                mapOptions = GetCommonMapOptions(),
                layers = GetLayers(false, false, true, false, false, false) // Include live bus locations only
            };
        }

        public object GetStaffViewLiveBusShiftConfiguration()
        {
            return new
            {
                googleMapsApiKey = _configuration["GoogleMaps:ApiKey"],
                initialCameraPosition = GetInitialCameraPosition(),
                mapOptions = GetCommonMapOptions(),
                layers = GetLayers(true, false, false, false, false, false)
            };
        }

        public object GetPassengerViewLiveBusRouteConfiguration()
        {
            return new
            {
                googleMapsApiKey = _configuration["GoogleMaps:ApiKey"],
                initialCameraPosition = GetInitialCameraPosition(),
                mapOptions = GetCommonMapOptions(),
                layers = GetLayers(true, false, false, true, false, true)
            };
        }

        public object GetPassengerViewLiveLocation()
        {
            return new
            {
                googleMapsApiKey = _configuration["GoogleMaps:ApiKey"],
                initialCameraPosition = GetInitialCameraPosition(),
                mapOptions = GetCommonMapOptions(),
                layers = GetLayers(false, false, false, true, false, false)
            };
        }
    }
}