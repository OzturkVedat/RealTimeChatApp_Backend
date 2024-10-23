using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using MongoDbGenericRepository.Attributes;

namespace RealTimeChatApp.API.Models
{
    [CollectionName("chats")]
    public class ChatModel
    {
        public ObjectId Id { get; set; }
        public List<string> ParticipantIds { get; set; }
        public string LastMessageContent { get; set; } = "No messages sent yet.";
        public string LastMessageSenderFullname {  get; set; }= string.Empty;
        public List<ObjectId> MessageIds { get; set; } = [];


        public ChatModel() { }
        public ChatModel(List<string> userIds)
        {
            ParticipantIds = userIds;
        }
    }

}
