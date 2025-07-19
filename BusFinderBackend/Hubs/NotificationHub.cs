using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace BusFinderBackend.Hubs
{
    public class NotificationHub : Hub
    {
        // Placeholder for sending a notification to a group
        public async Task SendNotificationToGroup(string groupName, string message)
        {
            await Clients.Group(groupName).SendAsync("ReceiveNotification", message);
        }
    }
} 