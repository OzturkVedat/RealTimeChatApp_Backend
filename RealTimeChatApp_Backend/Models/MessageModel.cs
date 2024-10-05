using MongoDB.Bson;
using MongoDbGenericRepository.Attributes;

namespace RealTimeChatApp.API.Models
{
    public class MessageModel
    {
        public ObjectId Id { get; set; }  
        public string SenderId { get; set; }
        public string Content { get; set; }
        public DateTime SentAt { get; set; }
    }

    [CollectionName("privateMessages")]
    public class PrivateMessage: MessageModel
    {
        public string RecipientId {  get; set; }
        public bool ReadStatus {  get; set; }

        public PrivateMessage(string senderId,string recipientId, string content)
        {
            SenderId = senderId;
            RecipientId = recipientId;
            Content= content;
            SentAt= DateTime.UtcNow;
            ReadStatus = false;
        }
    }

    [CollectionName("groupMessages")]
    public class GroupMessage : MessageModel
    {
        public List<string> RecipientIds { get; set; }
        public Dictionary<string, bool> ReadStatus { get; set; }        // map user IDs to their read status

        public GroupMessage(string senderId, List<string> recipientIds, string content  )
        {
            SenderId = senderId;
            RecipientIds = recipientIds;
            Content = content;
            SentAt= DateTime.UtcNow;
            foreach (var recipientId in recipientIds)
            {
                ReadStatus[recipientId] = false;    // Set to false for all users
            }
        }
    }

}
