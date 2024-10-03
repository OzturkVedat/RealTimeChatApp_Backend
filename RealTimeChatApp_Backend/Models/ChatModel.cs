using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using MongoDbGenericRepository.Attributes;

namespace RealTimeChatApp.API.Models
{
    [CollectionName("chats")]
    public class ChatModel
    {
        public ObjectId Id { get; set; }
        public List<string> UserIds { get; set; }
        public string LastMessage { get; set; }
        public DateTime LastMessageTime { get; set; }
        public DateTime CreatedAt { get; set; }
    }
    
}
