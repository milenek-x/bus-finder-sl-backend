using Microsoft.AspNetCore.SignalR;

namespace BusFinderBackend.Hubs
{
    public class BusesInRouteHub : Hub
    {
        public async Task SendBusesInRouteUpdate(string routeId, List<string> busIds, List<(double latitude, double longitude)> locations)
        {
            // Send the actual GPS coordinates of all buses in the specified route
            await Clients.All.SendAsync("BusesInRouteUpdated", routeId, busIds, locations);
        }
    }
} 