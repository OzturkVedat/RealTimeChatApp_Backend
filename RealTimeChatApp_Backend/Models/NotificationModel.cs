using MongoDB.Bson;

namespace RealTimeChatApp.API.Models
{
    public class NotificationModel
    {
        public ObjectId Id { get; set; }
        public string UserId {  get; set; }
        public string Message {  get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsRead {  get; set; }
        public NotificationType Type { get; set; }
    }
    public enum NotificationType
    {
        NewMessage,
        GroupInvite,
    }
}
