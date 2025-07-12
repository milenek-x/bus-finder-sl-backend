using Microsoft.AspNetCore.SignalR;

namespace BusFinderBackend.Hubs
{
    public class BusHub : Hub
{
    public async Task SendBusLocationUpdate(string busId, double latitude, double longitude)
    {
        // Send the actual GPS coordinates, not just a text message
        await Clients.All.SendAsync("BusLocationUpdated", busId, latitude, longitude);
    }
}
} 