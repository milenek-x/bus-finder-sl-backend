using Microsoft.AspNetCore.SignalR;

namespace BusFinderBackend.Hubs
{
    public class SingleBusHub : Hub
    {
        public async Task SendSingleBusUpdate(string busId, double latitude, double longitude)
        {
            // Send the actual GPS coordinates of the single bus
            await Clients.All.SendAsync("SingleBusUpdated", busId, latitude, longitude);
        }
    }
} 