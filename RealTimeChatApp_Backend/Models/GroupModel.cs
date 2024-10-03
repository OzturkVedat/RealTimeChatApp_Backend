using MongoDB.Bson;
using MongoDbGenericRepository.Attributes;

namespace RealTimeChatApp.API.Models
{
    [CollectionName("groups")]
    public class GroupModel
    {
        public ObjectId Id { get; set; }
        public string GroupName { get; set; }
        public List<string> UserIds { get; set; }
        public string AdminId {  get; set; }
        public DateTime CreatedAt { get; set; }
        public List<MessageModel> Messages { get; set; }
    }
}
