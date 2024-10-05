//using Microsoft.AspNetCore.Identity;
//using Microsoft.AspNetCore.SignalR;
//using MongoDB.Bson;
//using MongoDB.Driver;
//using RealTimeChatApp.API.Models;
//using RealTimeChatApp.API.Hubs;
//using RealTimeChatApp.API.Services;

//namespace RealTimeChatApp.API.Hubs
//{
//    public sealed class NotificationsHub : Hub
//    {
//        private readonly NotificationService _notificationService;
//        private readonly GroupService _groupService;
//        private readonly UserManager<UserModel> _userManager;

//        public NotificationsHub(NotificationService notificationService, GroupService groupService, UserManager<UserModel> userManager)
//        {
//            _notificationService = notificationService;
//            _groupService = groupService;
//            _userManager = userManager;
//        }

//        public async Task GetNotifications()
//        {
//            var userId = Context.UserIdentifier;
//            if (string.IsNullOrEmpty(userId))
//            {
//                await Clients.Caller.SendAsync("ReceiveMessage", "System", "User is not authenticated.");
//                return;
//            }

//            var notifications = await _notificationService.GetUnreadNotificationsAsync(userId);

//            await Clients.Caller.SendAsync("ReceiveAllNotifications", notifications);
//        }

//        public async Task SendNotificationToUser(string userId, string message, NotificationType type)
//        {
//            if (string.IsNullOrEmpty(userId))
//            {
//                await Clients.Caller.SendAsync("ReceiveMessage", "System", "Invalid user ID.");
//                return;
//            }

//            var user = await _userManager.FindByIdAsync(userId);
//            if (user == null)
//            {
//                await Clients.Caller.SendAsync("ReceiveMessage", "System", "User not found.");
//                return;
//            }

//            var notification = await _notificationService.CreateNotificationAsync(userId, message, type);
//            await Clients.User(userId).SendAsync("ReceiveNotification", notification);
//        }

//        public async Task SendNotificationToGroup(string groupId, string messageContent, NotificationType type)
//        {

//            var roomUserIds = await _groupService.GetUserIdsInGroupAsync(groupId);

//            if (roomUserIds.Count == 0)
//            {
//                await Clients.Caller.SendAsync("ReceiveMessage", "System", "No users in the group.");
//                return;
//            }

//            var tasks = roomUserIds.Select(async userId =>
//            {
//                var notification = await _notificationService.CreateNotificationAsync(userId, messageContent, type);
//                await Clients.User(userId).SendAsync("ReceiveNotification", notification);
//            });

//            await Task.WhenAll(tasks);
//        }
//        public async Task MarkNotificationAsRead(string notificationId)
//        {
//            if (!ObjectId.TryParse(notificationId, out var objectId))
//            {
//                await Clients.Caller.SendAsync("ReceiveMessage", "System", "Invalid notification ID.");
//                return;
//            }
//            var isUpdated = await _notificationService.MarkNotificationAsReadAsync(objectId);
//            if (isUpdated)
//                await Clients.Caller.SendAsync("NotificationRead", notificationId);

//            else
//                await Clients.Caller.SendAsync("ReceiveMessage", "System", "Notification not found or already read.");
//        }

//    }

//}
