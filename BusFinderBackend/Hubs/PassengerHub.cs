using Microsoft.AspNetCore.SignalR;

namespace BusFinderBackend.Hubs
{
    public class PassengerHub : Hub
    {
        public async Task SendPassengerLocationUpdate(string passengerId, double latitude, double longitude)
        {
            // Send the actual GPS coordinates of the passenger
            // In your SignalR Hub
            await Clients.All.SendAsync("PassengerLocationUpdated", passengerId, latitude, longitude);
        }
    }
} 