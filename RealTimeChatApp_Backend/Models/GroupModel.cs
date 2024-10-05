using MongoDB.Bson;
using MongoDbGenericRepository.Attributes;

namespace RealTimeChatApp.API.Models
{
    [CollectionName("groups")]
    public class GroupModel
    {
        public ObjectId Id { get; set; }
        public string GroupName { get; set; }
        public string Description {  get; set; }
        public string AdminId {  get; set; }
        public DateTime CreatedAt { get; set; }
        public ObjectId ChatId { get; set; }

        public GroupModel(string createdById, string groupName,  string description = "")
        {
            AdminId = createdById;
            GroupName = groupName;
            Description = description;
            CreatedAt = DateTime.UtcNow;
            ChatId = ObjectId.GenerateNewId(); 
        }
    }

}
