using MongoDB.Bson;
using MongoDbGenericRepository.Attributes;

namespace RealTimeChatApp.API.Models
{
    [CollectionName("messages")]
    public class MessageModel
    {
        public ObjectId Id { get; set; }  
        public string SenderId { get; set; }
        public List<string> RecipientIds { get; set; }      // one for private, many for group messages
        public string Content { get; set; }
        public DateTime SentAt { get; set; }
        public Dictionary<string,bool> ReadStatus {  get; set; }        // map user IDs to their read status
    }

}
