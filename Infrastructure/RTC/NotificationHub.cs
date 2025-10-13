
using MyApp.Infrastructure.Data;

using MyApp.Domain.Entities;
using Microsoft.AspNetCore.SignalR;

namespace MyApp.Infrastructure.RTC
{
    public class NotificationHub : Hub
    {
        private readonly AppDbContext _context;

        public NotificationHub(AppDbContext context)
        {
            _context = context;
        }

        // Method to send notification to all clients and save in DB
        public async Task SendNotification(string userName, string message)
        {
            // Save to database
            var notification = new Notification
            {
                UserName = userName,
                Message = message
            };

            _context.Notification.Add(notification);
            await _context.SaveChangesAsync();

            // Send to all connected clients
            await Clients.All.SendAsync("ReceiveNotification", userName, message, notification.Id);
        }

    }
}
