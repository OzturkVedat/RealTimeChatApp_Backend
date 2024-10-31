using MongoDB.Bson;
using MongoDbGenericRepository.Attributes;

namespace RealTimeChatApp.API.Models
{
    [CollectionName("messages")]
    public class MessageModel
    {
        public ObjectId Id { get; set; }  
        public string SenderId { get; set; }
        public string SenderFullname {  get; set; }
        public string Content { get; set; }
        public DateTime SentAt { get; set; }

        public MessageModel(string senderId,string senderFullname, string content)
        {
            SenderId = senderId;
            SenderFullname = senderFullname;
            Content = content;
            SentAt = DateTime.UtcNow;
        }
    }

}
