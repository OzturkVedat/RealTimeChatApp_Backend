using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using MongoDB.Bson;
using MongoDB.Driver;
using RealTimeChatApp.API.Models;
using RealTimeChatApp.API.Hubs;
using RealTimeChatApp.API.Services;

namespace RealTimeChatApp.API.Hubs
{
    public sealed class NotificationsHub : Hub
    {
        private readonly IMongoCollection<NotificationModel> _notificationsCollection;
        private readonly IMongoCollection<GroupModel> _groupsCollection;
        private readonly HubHelperService _helperService;
        private readonly UserManager<UserModel> _userManager;

        public NotificationsHub(IMongoDatabase mongoDb, HubHelperService helperService, UserManager<UserModel> userManager)
        {
            _notificationsCollection = mongoDb.GetCollection<NotificationModel>("notifications");
            _groupsCollection = mongoDb.GetCollection<GroupModel>("groups");
            _helperService = helperService;
            _userManager = userManager;
        }

        public async Task GetNotifications()
        {
            var userId = Context.UserIdentifier;
            if (string.IsNullOrEmpty(userId))
            {
                return;
            }

            var notifications = await _notificationsCollection
                .Find(n => n.UserId == userId && !n.IsRead)
                .SortByDescending(n => n.CreatedAt)
                .ToListAsync();

            await Clients.Caller.SendAsync("ReceiveAllNotifications", notifications);
        }

        public async Task SendNotificationToUser(string userId, string message, NotificationType type)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                await Clients.Caller.SendAsync("ReceiveMessage", "System", "User not found.");
                return;
            }
            var notification = new NotificationModel
            {
                UserId = userId,
                Message = message,
                Type = type,
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            };
            await _notificationsCollection.InsertOneAsync(notification);

            await Clients.User(userId).SendAsync("ReceiveNotification", notification);
        }

        public async Task SendNotificationToGroup(string groupId, string messageContent, NotificationType type)
        {

            var roomUserIds = await _helperService.GetUserIdsInGroupAsync(groupId);

            if (roomUserIds.Count == 0)
            {
                return;
            }

            foreach (var userId in roomUserIds)
            {
                var notification = new NotificationModel
                {
                    UserId = userId,
                    Message = messageContent,
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false,
                    Type = type
                };

                // Save the notification to the database
                await _notificationsCollection.InsertOneAsync(notification);

                // Send real-time notification to each user in the group (not to the sender)
                await Clients.User(userId).SendAsync("ReceiveNotification", notification);
            }
        }
        public async Task MarkNotificationAsRead(string notificationId)
        {
            var filter = Builders<NotificationModel>.Filter.Eq(n => n.Id, new ObjectId(notificationId));
            var update = Builders<NotificationModel>.Update.Set(n => n.IsRead, true);

            // Update the notification in the database
            var result = await _notificationsCollection.UpdateOneAsync(filter, update);

            if (result.ModifiedCount > 0)
            {
                await Clients.Caller.SendAsync("NotificationRead", notificationId);
            }
        }

    }

}
