using MongoDB.Bson;
using MongoDB.Driver;
using RealTimeChatApp.API.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RealTimeChatApp.API.Services
{
    public class NotificationService
    {
        private readonly IMongoCollection<NotificationModel> _notificationsCollection;

        public NotificationService(IMongoDatabase mongoDb)
        {
            _notificationsCollection = mongoDb.GetCollection<NotificationModel>("notifications");
        }

        public async Task<List<NotificationModel>> GetUnreadNotificationsAsync(string userId)
        {
            var filter = Builders<NotificationModel>.Filter.Eq(n => n.UserId, userId) &
                         Builders<NotificationModel>.Filter.Eq(n => n.IsRead, false);

            return await _notificationsCollection
                .Find(filter)
                .SortByDescending(n => n.CreatedAt)
                .ToListAsync();
        }

        public async Task<NotificationModel> CreateNotificationAsync(string userId, string message, NotificationType type)
        {
            var notification = new NotificationModel
            {
                UserId = userId,
                Message = message,
                Type = type,
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            };

            await _notificationsCollection.InsertOneAsync(notification);
            return notification;
        }

        public async Task<bool> MarkNotificationAsReadAsync(ObjectId notificationId)
        {
            var filter = Builders<NotificationModel>.Filter.Eq(n => n.Id, notificationId) &
                         Builders<NotificationModel>.Filter.Eq(n => n.IsRead, false);

            var update = Builders<NotificationModel>.Update.Set(n => n.IsRead, true);
            var result = await _notificationsCollection.UpdateOneAsync(filter, update);

            return result.ModifiedCount > 0;
        }
    }
}
