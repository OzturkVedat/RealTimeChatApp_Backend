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
        public List<string> RecipientIds { get; set; }      // single or plural
        public string Content { get; set; }
        public DateTime SentAt { get; set; }
        public bool ReadStatus { get; set; }

        public MessageModel(string senderId,string senderFullname, List<string>recipientIds, string content)
        {
            SenderId = senderId;
            SenderFullname = senderFullname;
            RecipientIds = recipientIds;
            Content = content;
            SentAt = DateTime.UtcNow;
            ReadStatus = false;
        }
    }

}
